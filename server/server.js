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
var colors = {};
var currentColors = {};
var colorTranslations = {};
var uiNameTranslations = {};

if(!fs.existsSync("./logs")) {
	fs.mkdirSync("./logs");
}

var logBuffer;
var oldDate = "";

function log(socket, data) {
	let port = "?????";
	if(socket.hasOwnProperty("BLPort")) {
		port = socket.BLPort.toString();
	}

	let d = new Date();
	let time = d.toString().split(" ")[4];
	let date = d.toJSON().toString().split("T")[0];

	if(oldDate != date) {
		if(logBuffer) {
			logBuffer.end();
		}
		logBuffer = fs.createWriteStream(`./logs/${date}.log`, {flags: "a"});
	}
	oldDate = date;

	let out = `[${port}, ${time}] ${data}`;
	logBuffer.write(out + "\r\n");
	console.log(out);
}

var funcs = {
	"connect": function(socket, parts) {
		return "HELLO";
	},

	"serverPort": function(socket, parts) {
		socket.BLPort = parseInt(parts[1]);

		if(!bricks.hasOwnProperty(socket.BLPort)) {
			bricks[socket.BLPort] = {};
			log(socket, `created vault`);
		}

		if(!colors.hasOwnProperty(socket.BLPort)) {
			colors[socket.BLPort] = {};
			log(socket, `created storage colorset`);
		}

		if(!currentColors.hasOwnProperty(socket.BLPort)) {
			currentColors[socket.BLPort] = {};
			log(socket, `created active colorset`);
		}

		if(!colorTranslations.hasOwnProperty(socket.BLPort)) {
			colorTranslations[socket.BLPort] = {};
			log(socket, `created colorset translation table`);
		}

		if(!uiNameTranslations.hasOwnProperty(socket.BLPort)) {
			uiNameTranslations[socket.BLPort] = {};
			log(socket, `created ui name translation table`);
		}

		socket.write("needUINames\r\n");
		socket.write("needColors\r\n");
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

	"eventClear": function(socket, parts) {
		const uniq = parts[1];

		var b = bricks[socket.BLPort];

		if(!b.hasOwnProperty(uniq)) {
			b[uniq] = {};
		}

		if(!b[uniq].hasOwnProperty("events")) {
			b[uniq].events = {};
		}
		let e = b[uniq].events;
		for(let idx in e) {
			delete e[idx];
		}
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

		b[uniq][parts[2]] = (typeof parts[3] === "undefined" ? "" : parts[3]);
	},

	"delete": function(socket, parts) {
		const uniq = parts[1];

		var b = bricks[socket.BLPort];

		if(!b.hasOwnProperty(uniq)) {
			return;
		}

		delete b[uniq];
	},

	"colorsetLength": function(socket, parts) {
		let c = currentColors[socket.BLPort];
		for(let idx in c) {
			delete c[idx];
		}
	},

	"colorset": function(socket, parts) {
		var c = currentColors[socket.BLPort];
		c[parts[1]] = parts[2];
	},

	"colorsetEnd": function(socket, parts) {
		fs.readdir("./saves", function(err, files) {
			let highest = 0;
			let highestFile = "";

			for(let idx in files) {
				let fparts = files[idx].split("-");

				let ext = fparts[1].split(".")[1];
				if(ext != "blls") {
					continue;
				}

				let timestamp = parseInt(fparts[0]);
				let port = parseInt(fparts[1]);

				if(timestamp > highest && port == socket.BLPort) {
					highest = timestamp;
					highestFile = files[idx];
				}
			}

			if(highestFile) {
				loadBLLS(socket, `./saves/${highestFile}`);
			}

			socket.write("okToLoad\r\n");
		});
	},

	"save": function(socket, parts) {
		if(!fs.existsSync("./saves")) {
			fs.mkdirSync("./saves");
		}
		//fs.writeFileSync(`./saves/${Date.now()}-${socket.BLPort}.json`, JSON.stringify(bricks[socket.BLPort]), "utf8");

		exportBLLS(socket);
	},

	"clear": function(socket, parts) {
		if(parts.length > 1) {
			if(parseInt(parts[1])) {
				exportBLLS(socket);
			}
		}

		var b = bricks[socket.BLPort];
		for(uniq in b) {
			delete b[uniq];
		}
	},

	"loadBLLS": function(socket, parts) {
		let filename = `./saves/${parts[1]}`;
		if(!fs.existsSync(filename)) {
			return;
		}

		exportBLLS(socket);
		var b = bricks[socket.BLPort];
		for(uniq in b) {
			delete b[uniq];
		}

		loadBLLS(socket, filename);
	},

	"uiname": function(socket, parts) {
		uiNameTranslations[socket.BLPort][parts[1]] = parts[2].replace(/\[DEG\]/g, "\xb0");
	},

	"saveBLS": function(socket, parts) {
		exportBLS(socket);
	}
};

function sendBrick(socket, uniq) {
	var b = bricks[socket.BLPort];

	if(!b.hasOwnProperty(uniq)) {
		log(socket, `failed to send brick ${uniq}, not vaulted`);
		return;
	}

	let parts = [
		uniq,
		b[uniq].name,
		b[uniq].owner,
		b[uniq].angleID,
		b[uniq].colorFxID,
		b[uniq].shapeFxID,
		b[uniq].colorID,
		b[uniq].dataBlock,
		b[uniq].position,
		b[uniq].rotation,
		b[uniq].print,
		b[uniq].light,
		b[uniq].music,
		b[uniq].attr,		
	];
	socket.write(`brick\t${parts.join("\t")}\r\n`);

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

function emulateCM3(socket) {
	for(let z in colorTranslations[socket.BLPort]) {
		delete colorTranslations[socket.BLPort][z]
	}

	let c = colors[socket.BLPort];
	let cc = currentColors[socket.BLPort];
	let ct = colorTranslations[socket.BLPort];

	log(socket, `save colors\r\n${Object.values(c).join(", ")}`);
	log(socket, `current colors\r\n${Object.values(cc).join(", ")}`);

	for(let idx in c) {
		color = c[idx].split(" ").map(parseFloat);

		red = color[0];
		green = color[1];
		blue = color[2];
		alpha = color[3];

		if(alpha >= 0.0001) {
			let minDiff = 99999;
			let matchIdx = -1;

			for(let j in cc) {
				let checkColor = cc[j].split(" ").map(parseFloat);

				let checkRed = checkColor[0];
				let checkGreen = checkColor[1];
				let checkBlue = checkColor[2];
				let checkAlpha = checkColor[3];

				let diff = 0;
				diff += Math.abs(Math.abs(checkRed) - Math.abs(red));
				diff += Math.abs(Math.abs(checkGreen) - Math.abs(green));
				diff += Math.abs(Math.abs(checkBlue) - Math.abs(blue));

				if(checkAlpha > 0.99 && alpha < 0.99 || (checkAlpha < 0.99 && alpha > 0.99)) {
					diff += 1000;
				} else {
					diff += (Math.abs(Math.abs(checkAlpha) - Math.abs(alpha))) * 0.5;
				}

				if (diff < minDiff)
				{
					minDiff = diff;
					matchIdx = j;
				}
			}

			if(matchIdx == -1) {
				matchIdx = 0;
			} else {
				ct[idx] = matchIdx;
			}
		}
	}

	log(socket, `color translation table\r\n${Object.values(ct).join(", ")}`);
}

function exportBLS(socket) {
	let c = colors[socket.BLPort];
	let b = bricks[socket.BLPort];
	let fn = `./saves/${Date.now()}-${socket.BLPort}.bls`;
	// blockland specifically wants latin1/ISO-8859-1/ANSI here
	let stream = fs.createWriteStream(fn, {encoding: "latin1"});

	stream.write(`This is a Blockland save file.  You probably shouldn't modify it cause you'll screw it up.\r\n1\r\nLiveSaver autosave from server port ${socket.BLPort} ts ${Date.now()}\r\n`);

	for(let idx = 0; idx < 64; idx++) {
		let color = (idx < Object.keys(c).length ? c[idx] : "1.000000 0.000000 1.000000 0.000000");
		stream.write(`${color}\r\n`);
	}

	stream.write(`Linecount ${Object.keys(b).length}\r\n`);

	/*
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
	*/

	// why is everything a baseplate?
	//    needs to be sorted bottom to top first, also need to start sending %brick.isBaseplate from in-game as parts[15]
	// why is the print always blank?
	//    i'm setting everything to "1x1/vent" as it's printID 0 from in-game, i have to fix that at some point

	for(let uniq in b) {
		let brick = b[uniq];
		let parts = [
			uiNameTranslations[socket.BLPort][brick.dataBlock] + "\"",
			brick.position,
			brick.angleID,
			1,
			brick.colorID,
			"",
			brick.colorFxID,
			brick.shapeFxID,
			brick.attr
		];
		stream.write(`${parts.join(" ")}\r\n`);
		stream.write(`+-OWNER ${brick.owner}\r\n`);
	}

	stream.end();
	log(socket, `saved BLS to ${fn}`);
}

function exportBLLS(socket, fnadd = "") {
	let c = currentColors[socket.BLPort];
	let b = bricks[socket.BLPort];
	let fn = `./saves/${Date.now()}-${socket.BLPort}${(fnadd == "" ? "" : `-${fnadd}`)}.blls`;
	let stream = fs.createWriteStream(fn);

	for(let idx in c) {
		stream.write(`${c[idx]}\r\n`);
	}

	let owners = {};

	let skipIdx = [1, 6];
	for(let uniq in b) {
		let brick = b[uniq];
		
		if(!owners.hasOwnProperty(brick.owner)) {
			owners[brick.owner] = {};
		}
		let group = owners[brick.owner];

		if(!group.hasOwnProperty(brick.dataBlock)) {
			group[brick.dataBlock] = [];
		}
		let dbGroup = group[brick.dataBlock];

		let data = [
			b[uniq].name,
			b[uniq].angleID,
			b[uniq].colorFxID,
			b[uniq].shapeFxID,
			b[uniq].colorID,
			b[uniq].position,
			b[uniq].rotation,
			b[uniq].print,
			b[uniq].light,
			b[uniq].music,
			b[uniq].attr,		
		];

		dbGroup[uniq] = data;
	}

	for(let owner in owners) {
		stream.write(`OWN\t${owner}\r\n`);
		let group = owners[owner];
		for(let db in group) {
			let dbGroup = group[db];
			stream.write(`DB\t${db}\r\n`);
			for(let uniq in dbGroup) {
				let data = dbGroup[uniq];
				stream.write(`${uniq}\t${data.join("\t")}\r\n`);

				if(b[uniq].hasOwnProperty("emitter")) {
					if(b[uniq].emitter != "") {
						stream.write(`M\t${b[uniq].emitter}\t${b[uniq].emitterDir}\r\n`);
					}
				}
				if(b[uniq].hasOwnProperty("item")) {
					if(b[uniq].item != "") {
						stream.write(`I\t${b[uniq].item}\t${b[uniq].itemDir}\t${b[uniq].itemPos}\t${b[uniq].itemTime}\r\n`);
					}
				}
				if(b[uniq].hasOwnProperty("vehicle")) {
					if(b[uniq].vehicle != "") {
						stream.write(`V\t${b[uniq].vehicle}\t${b[uniq].colorVehicle}\r\n`);
					}
				}
				if(b[uniq].hasOwnProperty("events")) {
					for(let idx in b[uniq].events) {
						let e = b[uniq].events[idx];
						let ev = Object.values(e);

						stream.write(`E\t${idx}\t${ev.slice(0, 6).join("\t")}\t${(e.params.length > 0 ? e.params.join("\t") : "")}\r\n`);
					}
				}
			}
		}
	}

	log(socket, `saved to ${fn}`);
	stream.end();
}

function loadBLLS(socket, file) {
	log(socket, `attempting to load BLLS ${file}`);

	for(let z in colors[socket.BLPort]) {
		delete colors[socket.BLPort][z]
	}

	let c = colors[socket.BLPort];
	let b = bricks[socket.BLPort];
	let ct = colorTranslations[socket.BLPort];
	let cc = currentColors[socket.BLPort];

	fs.readFile(file, "utf8", function(err, data) {
		if(err) {
			log(socket, `Unable to load recent save\r\n${err}`);
			socket.write("okToProcess\r\n");
			return;
		}

		let lines = data.split("\n").map(x => x.trim());

		let startedBricks = false;
		let owner = -1;
		let db = -1;
		let uniq = -1;

		for(let idx in lines) {
			let line = lines[idx];
			if(!line) {
				continue;
			}
			let parts = line.split("\t");

			switch(parts[0]) {
				case "OWN":
					owner = parts[1];
					if(!startedBricks) {
						emulateCM3(socket);
					}
					startedBricks = true;
					break;

				case "DB":
					db = parts[1];
					break;

				case "M":
					parts.splice(1, 0, uniq);
					funcs["brickEmitter"](socket, parts);
					break;

				case "I":
					parts.splice(1, 0, uniq);
					funcs["brickItem"](socket, parts);
					break;

				case "V":
					parts.splice(1, 0, uniq);
					funcs["brickVehicle"](socket, parts);
					break;

				case "E":
					parts.splice(1, 0, uniq);
					funcs["brickEvent"](socket, parts);
					break;

				default:
					if(startedBricks) {
						uniq = parts[0];
						parts.splice(0, 0, "brick");
						parts.splice(3, 0, owner);
						parts.splice(8, 0, db);
						parts[7] = ct[parts[7]];
						funcs["brick"](socket, parts);
					} else {
						c[Object.values(c).length] = line;
					}
					break;
			}
		}

		socket.write("okToProcess\r\n");
	});
}

function saveOnAllSockets() {
	for(let idx in TCPClients) {
		let socket = TCPClients[idx];
		exportBLLS(socket);
	}
}
setInterval(saveOnAllSockets, settings.saveInterval*60*1000);

function handle(socket, parts) {
	if(!parts.length) {
		return;
	}

	let cmd = parts[0];

	if(!cmd) {
		return;
	}

	log(socket, parts.join(" "));
	//console.log(parts.join(" "));

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

if(!settings.devMode) {
	process.prependListener('uncaughtException', function(e) {
		for(let idx in TCPClients) {
			let socket = TCPClients[idx];
			exportBLLS(socket, "exceptionThrown");
		}
	});
}

var TCPClients = [];
net.createServer(function(socket) {
	TCPClients.push(socket);
	socket.write("OK\r\n");

	socket.on("data", function(data) {
		let lines = data.toString().split("\n").map(x => x.trim());
		for(let idx in lines) {
			let line = lines[idx].toString().trim();

			if(!line) {
				return;
			}

			var parts = line.split("\t").map(x => x.trim());
			handle(socket, parts);
		}
	});

	socket.on("error", function(err) {
		TCPClients.splice(TCPClients.indexOf(socket), 1);
	});

	socket.on("end", function(err) {
		TCPClients.splice(TCPClients.indexOf(socket), 1);
	});
}).listen(settings.net.port, "127.0.0.1");