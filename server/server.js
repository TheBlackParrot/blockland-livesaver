const net = require("net");
const fs = require("fs");
const settings = require("./settings.json");

// https://stackoverflow.com/a/23013726
function swap(json){
	var ret = {};
	for(var key in json) {
		ret[json[key]] = key;
	}
	return ret;
}

var bricks = {};

var funcs = {
	"connect": function(socket, parts) {
		// connect
		teams = {};
		return "HELLO";
	},

	"serverPort": function(socket, parts) {
		socket.BLPort = parseInt(parts[1]);

		socket.write("okToLoad\r\n");

		if(!bricks.hasOwnProperty(socket.BLPort)) {
			bricks[socket.BLPort] = {};
			console.log(`created vault for Blockland server port ${socket.BLPort}`);
		}
	},

	"brick": function(socket, parts) {
		/*
			"brick" TAB
			%brick._LS_uniq TAB
			%brick.getName() TAB
			%brick.getGroup().bl_id TAB
			%brick.angleID TAB
			%brick.colorFxID TAB
			%brick.shapeFxID TAB
			%brick.colorID TAB
			%brick.dataBlock TAB
			%brick.getPosition() TAB
			%brick.rotation TAB
			_LSgetPrintName(%brick.printID)
		*/
		const uniq = parts[1];

		var b = bricks[socket.BLPort];

		if(!b.hasOwnProperty(uniq)) {
			b[uniq] = {};
		}

		b[uniq].name = parts[2];
		b[uniq].owner = parts[3];
		b[uniq].angleID = parts[4];
		b[uniq].colorFxID = parts[5];
		b[uniq].shapeFxID = parts[6];
		b[uniq].colorID = parts[7];
		b[uniq].dataBlock = parts[8];
		b[uniq].position = parts[9];
		b[uniq].rotation = parts[10];
		b[uniq].print = parts[11];
		b[uniq].light = parts[12];
		b[uniq].music = parts[13];
		b[uniq].attr = parts[14];
	},

	"brickEmitter": function(socket, parts) {
		const uniq = parts[1];

		var b = bricks[socket.BLPort];

		if(!b.hasOwnProperty(uniq)) {
			b[uniq] = {};
		}

		b[uniq].emitter = parts[2];
		b[uniq].emitterDir = parts[3];
	},

	"brickItem": function(socket, parts) {
		const uniq = parts[1];

		var b = bricks[socket.BLPort];

		if(!b.hasOwnProperty(uniq)) {
			b[uniq] = {};
		}

		b[uniq].item = parts[2];
		b[uniq].itemDir = parts[3];
		b[uniq].itemPos = parts[4];
		b[uniq].itemTime = parts[5];
	},

	"brickVehicle": function(socket, parts) {
		const uniq = parts[1];

		var b = bricks[socket.BLPort];

		if(!b.hasOwnProperty(uniq)) {
			b[uniq] = {};
		}

		b[uniq].vehicle = parts[2];
		b[uniq].colorVehicle = parts[3];	
	},

	"brickEvent": function(socket, parts) {
		const uniq = parts[1];

		var b = bricks[socket.BLPort];

		if(!b.hasOwnProperty(uniq)) {
			b[uniq] = {};
		}

		if(!b[uniq].hasOwnProperty("events")) {
			b[uniq].events = {};
		}
		let e = b[uniq].events;

		let idx = parts[2];
		e[idx] = {};
		e[idx].eventEnabled = parts[3];
		e[idx].eventInput = parts[4];
		e[idx].eventDelay = parts[5];
		e[idx].eventTarget = parts[6];
		e[idx].eventNT = parts[7];
		e[idx].eventOutput = parts[8];
		e[idx].params = parts.slice(9);
	},

	"load": function(socket, parts) {
		var b = bricks[socket.BLPort];

		socket.write(`beginLoad\r\n`);

		let idx = 0;
		for(let uniq in b) {
			let out = "";
			setTimeout(sendBrick, idx, socket, uniq);
			idx++;
		}

		setTimeout(function() {
			socket.write("endLoad\r\n");
		}, idx+1);
	},

	"brickUpdate": function(socket, parts) {
		const uniq = parts[1];

		var b = bricks[socket.BLPort];

		if(!b.hasOwnProperty(uniq)) {
			return;
		}

		b[uniq][parts[2]] = parts[3];
	},

	"delete": function(socket, parts) {
		const uniq = parts[1];

		var b = bricks[socket.BLPort];

		if(!b.hasOwnProperty(uniq)) {
			return;
		}

		delete b[uniq];
	},

	"save": function(socket, parts) {
		if(!fs.existsSync("./saves")) {
			fs.mkdirSync("./saves");
		}
		fs.writeFileSync(`./saves/${Date.now()}-${socket.BLPort}.json`, JSON.stringify(bricks[socket.BLPort]), "utf8");
	}
};
/*
				"brickEvent" TAB
				%brick._LS_uniq TAB
				%idx TAB
				%brick.eventEnabled[%idx] TAB
				%brick.eventInput[%idx] TAB
				%brick.eventDelay[%idx] TAB
				%brick.eventTarget[%idx] TAB
				%brick.eventNT[%idx] TAB
				%brick.eventOutput[%idx] TAB
				%params
*/

function sendBrick(socket, uniq) {
	var b = bricks[socket.BLPort];

	if(!b.hasOwnProperty(uniq)) {
		console.log(`failed to send brick ${uniq}, not vaulted`);
		return;
	}

	socket.write(`brick\t${uniq}\t` + Object.values(b[uniq]).slice(0, 14).join("\t") + "\r\n");

	if(b[uniq].hasOwnProperty("emitter")) {
		socket.write(`brickEmitter\t${uniq}\t${b[uniq].emitter}\t${b[uniq].emitterDir}\r\n`);
	}
	if(b[uniq].hasOwnProperty("item")) {
		socket.write(`brickItem\t${uniq}\t${b[uniq].item}\t${b[uniq].itemDir}\t${b[uniq].itemPos}\t${b[uniq].itemTime}\r\n`);
	}
	if(b[uniq].hasOwnProperty("vehicle")) {
		socket.write(`brickVehicle\t${uniq}\t${b[uniq].vehicle}\t${b[uniq].colorVehicle}\r\n`);
	}
	if(b[uniq].hasOwnProperty("events")) {
		for(let idx in b[uniq].events) {
			let e = b[uniq].events[idx];
			let ev = Object.values(e);

			socket.write(`brickEvent\t${uniq}\t${idx}\t${ev.slice(0, 6).join("\t")}\t${(e.params.length > 0 ? e.params.join("\t") : "")}\r\n`);
		}
	}
}

function handle(socket, parts) {
	if(!parts.length) {
		return;
	}

	let cmd = parts[0];

	if(!cmd) {
		return;
	}

	console.log("[" + socket.remotePort + "] " + parts.join(" "));

	let send = function(data) {
		socket.write(data + "\r\n");
	}

	let ready = true

	if(cmd in funcs) {
		if(!ready) {
			setTimeout(function() {
				handle(socket, parts);
				return;
			}, 1000);
		} else {
			let out = funcs[cmd](socket, parts);
			if(out) {
				send(out);
			}
		}
	}
}

var TCPClients = [];
net.createServer(function(socket) {
	TCPClients.push(socket);
	socket.write("OK\r\n");

	socket.on("data", function(data) {
		var parts = data.toString().split("\t").map(function(part) {
			return part.trim();
		});

		if(!(data.toString().trim())) {
			return;
		}

		handle(socket, parts);
	});

	socket.on("error", function(err) {
		TCPClients.splice(TCPClients.indexOf(socket), 1);
	});

	socket.on("end", function(err) {
		TCPClients.splice(TCPClients.indexOf(socket), 1);
	});
}).listen(settings.net.port, "127.0.0.1");