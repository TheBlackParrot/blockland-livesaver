function LiveSaverTCPLines::send(%this, %data) {
	%list = %this;
	if(!$Server::LS::Connected) {
		%list = LiveSaverTCPToProcess;
	}

	//%next = %list.getRowText(%list.rowCount()-1);

	//if(getField(%data, 0) $= getField(%next, 0) && getField(%data, 1) $= getField(%next, 1)
	//	&& getField(%data, 0) !$= "brickEvent"
	//	&& getField(%data, 0) !$= "brickUpdate")
	//{
	//	%list.removeRow(%list.rowCount()-1);
	//}

	%list.addRow(getSimTime(), %data);

	if(!isEventPending(%this.checkToSendSched)) {
		%this.checkToSend();
	}
}

$LSIgnoreChecks = true;
function LiveSaverTCPLines::checkToSend(%this) {
	%other = LiveSaverTCPToProcess;
	%list = %this;
	if(%other.rowCount() > 0 && %this.rowCount() <= 0 && $Server::LS::ProcessQueue) {
		%list = %other;
	}

	if(%other.rowCount() <= 0 && %this.rowCount() <= 0) {
		return;
	}

	//%bypass = "\tdelete\tserverPort\tconnect\tload\tdelete\tsave\tcolorset\tcolorsetLength";
	//if(!isObject($Server::LiveSaver[getField(%data, 1)]) && stripos(%bypass, "\t" @ getField(%data, 0) @ "\t") == -1) {
	//	return;
	//}

	if($Server::LS::Connected) {
		%this.checkToSendSched = %this.schedule(1, checkToSend);

		%data = %list.getRowText(0);
		%list.removeRow(0);

		LiveSaverTCPObject.send(%data @ "\r\n");
		//echo("\c5[SENT]\c0" SPC %data);
	}
}

function LiveSaverTCPObject::onLine(%this, %line) {
	%line = trim(%line);
	//echo("\c4[RECV]\c0" SPC %line);
	
	// if you host this to the outside world (WHICH YOU HAVE TO RECONFIGURE TO MAKE HAPPEN), don't come complaining to me when your server inevitably crashes.
	// i am WELL AWARE of the security risks here, leave it on 127.0.0.1 and stick to trying to crash yourself thanks
	%cmd = getField(%line, 0);
	call("_LSRCMD_" @ trim(%cmd), getFields(%line, 1));
}

function _LSRCMD_HELLO() {
	messageAll('', "\c6Connected to the LiveSaver server.");
	$Server::LS::Connected = true;

	LiveSaverTCPLines.send("serverPort" TAB $Pref::Server::Port);
}

function _LSRCMD_okToLoad() {
	if($LS::InitLoad $= "") {
		LiveSaverTCPLines.schedule(100, send, "load");
	}
	$LS::InitLoad = true;
}

function _LSRCMD_needUINames() {
	for(%i = 0; %i < DataBlockGroup.getCount(); %i++) {
		%db = DataBlockGroup.getObject(%i);
		if(%db.getClassName() !$= "fxDTSBrickData") {
			continue;
		}
		// BLS FORMAT NEEDS TO DIE
		// ALSO: TO HELL WITH RAMPS. >:C
		LiveSaverTCPLines.send("uiname" TAB %db.getName() TAB strReplace(%db.uiName, "\xb0", "[DEG]"));
		echo(strReplace(%db.uiName, "°", "[DEG]"));
	}
	LiveSaverTCPLines.send("uinameEnd");
}

function _LSRCMD_needColors() {
	%colors = _LSgetColorsetLength();
	LiveSaverTCPLines.send("colorsetLength" TAB %colors);
	for(%i = 0; %i < %colors; %i++) {
		LiveSaverTCPLines.send("colorset" TAB %i TAB getColorIDTable(%i));
	}
	LiveSaverTCPLines.send("colorsetEnd");
}

function _LSRCMD_okToProcess() {
	$Server::LS::ProcessQueue = true;
	LiveSaverTCPLines.checkToSend();
}

function _LSRCMD_beginLoad(%fields) {
	$Server::LSLoading = true;

	%quotaObj = getCurrentQuotaObject();
	clearCurrentQuotaObject();
	if(isObject(%quotaObj))
		setCurrentQuotaObject(%quotaObj);
}

function _LSRCMD_endLoad(%fields) {
	$Server::LSLoading = false;
}

function _LSRCMD_brick(%fields) {
	%uniq = getField(%fields, 0);
	%name = getField(%fields, 1);
	%owner = getField(%fields, 2);
	%light = getField(%fields, 11);
	%music = getField(%fields, 12);
	%raycast = getWord(getField(%fields, 13), 0);
	%collide = getWord(getField(%fields, 13), 1);
	%render = getWord(getField(%fields, 13), 2);

	if(%uniq > $Server::LSHighestUniq) {
		$Server::LSHighestUniq = %uniq;
	}

	%group = "BrickGroup_" @ %owner;
	if(!isObject(%group)) {
		%newGroup = new SimGroup(%group) {
			client = 0;
			name = "\c1BL_ID:" SPC %owner @ "\c0";
			bl_id = %owner;
		};
		mainBrickGroup.add(%newGroup);
	}

	%brick = new fxDTSBrick() {
		_LS_uniq = %uniq;
		angleID = getField(%fields, 3);
		colorFxID = getField(%fields, 4);
		shapeFxID = getField(%fields, 5);
		colorID = getField(%fields, 6);
		dataBlock = getField(%fields, 7);
		position = getField(%fields, 8);
		rotation = getField(%fields, 9);
		printID = $PrintNameTable[getField(%fields, 10)];
		isPlanted = 1;
		scale = "1 1 1";
		stackBL_ID = -1;
	};
	if(!isObject(%brick)) {
		warn("Failed to create brick" SPC %uniq);
		return;
	}

	%err = %brick.plant();
	if(%err == 1 || %err == 3 || %err == 5) {
		%brick.delete();
		return;
	}

	%group.add(%brick);
	%brick.setNTObjectName(%name);
	%brick.setTrusted(1);
	$Server::LiveSaver[%brick._LS_uniq] = %brick;
	//%group.addNTName(%name);

	if(%light !$= "") { %brick.setLight(%light); }
	if(%music !$= "") { %brick.setMusic(%music); }

	%brick.setRaycasting(%raycast);
	%brick.setColliding(%collide);
	%brick.setRendering(%render);

	%brick.isACTUALLYPlanted = true;
}

function _LSRCMD_brickEmitter(%fields) {
	%uniq = getField(%fields, 0);
	%brick = $Server::LiveSaver[%uniq];

	if(!isObject(%brick)) {
		warn("Can't set attribute on brick" SPC %brick @ ", doesn't exist");
		return;
	}

	if(%uniq > $Server::LSHighestUniq) {
		$Server::LSHighestUniq = %uniq;
	}

	%brick.setEmitter(getField(%fields, 1));
	%brick.setEmitterDirection(getField(%fields, 2));
}

function _LSRCMD_brickItem(%fields) {
	%uniq = getField(%fields, 0);
	%brick = $Server::LiveSaver[%uniq];

	if(!isObject(%brick)) {
		warn("Can't set attribute on brick" SPC %brick @ ", doesn't exist");
		return;
	}

	if(%uniq > $Server::LSHighestUniq) {
		$Server::LSHighestUniq = %uniq;
	}

	%brick.setItem(getField(%fields, 1));
	%brick.setItemDirection(getField(%fields, 2));
	%brick.setItemPosition(getField(%fields, 3));
	%brick.setItemRespawnTime(getField(%fields, 4));
}

function _LSRCMD_brickVehicle(%fields) {
	%uniq = getField(%fields, 0);
	%brick = $Server::LiveSaver[%uniq];

	if(!isObject(%brick)) {
		warn("Can't set attribute on brick" SPC %brick @ ", doesn't exist");
		return;
	}

	if(%uniq > $Server::LSHighestUniq) {
		$Server::LSHighestUniq = %uniq;
	}

	%vehicle = getField(%fields, 1);
	if(isObject(%vehicle)) {
		%brick.setVehicle(nameToID(getField(%fields, 1)));
		%brick.reColorVehicle = getField(%fields, 2);
	}
}

if(!isObject(LSDummyClient)) {
	new ScriptObject(LSDummyClient) {
		isAdmin = 1;
		wrenchBrick = 0;
	};
}

function _LSRCMD_brickEvent(%fields) {
	// https://github.com/Electrk/bl-decompiled/blob/5ea86a1e4f71cb92799bdc1305ba062771c5867e/server/scripts/allGameScripts/loadBricks/ServerLoadSaveFile_Tick.cs#L23

	%uniq = getField(%fields, 0);
	%brick = $Server::LiveSaver[%uniq];
	%idx = getField (%fields, 1);
	%enabled = getField (%fields, 2);
	%inputName = getField (%fields, 3);
	%delay = getField (%fields, 4);

	%targetName = getField (%fields, 5);
	%NT = getField (%fields, 6);

	%outputName = getField (%fields, 7);

	%par1 = getField (%fields, 8);
	%par2 = getField (%fields, 9);
	%par3 = getField (%fields, 10);
	%par4 = getField (%fields, 11);

	%inputEventIdx = inputEvent_GetInputEventIdx(%inputName);

	%targetIdx = inputEvent_GetTargetIndex("fxDTSBrick", %inputEventIdx, %targetName);

	if (%targetName == -1) {
		%targetClass = "fxDTSBrick";
	} else {
		%field = getField($InputEvent_TargetList["fxDTSBrick", %inputEventIdx], %targetIdx);
		%targetClass = getWord(%field, 1);
	}

	%outputEventIdx = outputEvent_GetOutputEventIdx(%targetClass, %outputName);
	%NTNameIdx = -1;

	for (%j = 0; %j < 4; %j++) {
		%field = getField($OutputEvent_parameterList[%targetClass, %outputEventIdx], %j);
		%dataType = getWord(%field, 0);

		if(%dataType $= "datablock") {
			if(%par[%j + 1] != -1 && !isObject(%par[%j + 1])) {
				warn("WARNING: could not find datablock for event " @ %outputName @ " -> " @ %par[%j + 1]);
			}
		}
	}

	$LoadingBricks_Client = LSDummyClient;
	LSDummyClient.wrenchBrick = $LastLoadedBrick = %brick;
	serverCmdAddEvent (LSDummyClient, %enabled, %inputEventIdx, %delay, %targetIdx, %NTNameIdx, %outputEventIdx, %par1, %par2, %par3, %par4);
	%brick.eventNT[%idx] = %NT;
}