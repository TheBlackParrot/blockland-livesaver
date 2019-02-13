exec("./math.cs");
exec("./support.cs");
exec("./vars.cs");

$Server::LSAddress = "127.0.0.1:" @ $Pref::Server::LSPort;
$Server::LS::Connected = false;

if($Server::LSHighestUniq $= "") {
	$Server::LSHighestUniq = 0;
}

function initLiveSaverConnection() {
	if(!isObject(LiveSaverTCPObject)) {
		new TCPObject(LiveSaverTCPObject);
	} else {
		LiveSaverTCPObject.disconnect();
	}

	%obj = LiveSaverTCPObject;
	%obj.connect($Server::LSAddress);

	if(!isObject(LiveSaverTCPLines)) {
		// blockland likes to merge multiple send commands being called at once into one line
		// "does this look stupid?" yes, but it's easy
		new GuiTextListCtrl(LiveSaverTCPLines);
	} else {
		LiveSaverTCPLines.clear();
	}

	if(!isObject(LiveSaverTCPToProcess)) {
		new GuiTextListCtrl(LiveSaverTCPToProcess);
	} else {
		LiveSaverTCPToProcess.clear();
	}
}
initLiveSaverConnection();

function LiveSaverTCPObject::onConnected(%this) {
	cancel($LiveSaverConnectRetryLoop);

	echo("Connected to the LiveSaver server.");
	LiveSaverTCPObject.send("connect\r\n");
}

function LiveSaverTCPObject::onConnectFailed(%this) {
	cancel($LiveSaverConnectRetryLoop);
	echo("Trying to connect to the LiveSaver server again (failed to connect)...");
	$Server::LS::Connected = false;
	$Server::LS::ProcessQueue = false;
	$LiveSaverConnectRetryLoop = %this.schedule(1000, connect, $Server::LSAddress);
}

function LiveSaverTCPObject::onDisconnect(%this) {
	cancel($LiveSaverConnectRetryLoop);
	echo("Trying to connect to the LiveSaver server again (disconnected)...");
	$Server::LS::Connected = false;
	$Server::LS::ProcessQueue = false;
	$LiveSaverConnectRetryLoop = %this.schedule(1000, connect, $Server::LSAddress);
}

exec("./communication.cs");
exec("./saving.cs");
exec("./live.cs");