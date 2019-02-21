// todo: remove the stupid %attr crap, it was 3am when i wrote that or something
function fxDTSBrick::sendLSUpdate(%this, %what, %rVal) {
	if(%this._LS_uniq $= "" || !isObject(%this) || %this == %this.client.player.tempbrick || %this.isACTUALLYPlanted $= "" || $Server::LSLoading) {
		return;
	}

	switch$(%what) {
		case "color":
			%attr = "colorID";
			%val = %this.getColorID();

		case "print":
			%attr = "print";
			%val = _LSgetPrintName(%rVal);

		case "item" or "emitter" or "light" or "vehicle" or "music":
			%attr = %what;
			%val = (isObject(%rVal) ? %rVal.getName() : "");

		case "emitterDir" or "itemDir" or "itemPos" or "itemTime" or "colorVehicle" or "attr" or "name" or "colorFxID" or "shapeFxID":
			%attr = %what;
			%val = %rVal;
	}

	if(%attr $= "") {
		return;
	}

	cancel(%this.updateDelay[%attr]);

	%data = "brickUpdate" TAB %this._LS_uniq TAB %attr TAB %val;
	%this.updateDelay[%attr] = %this.schedule(5000, pushLSUpdate, %data);
}

function fxDTSBrick::sendLSEventUpdate(%brick) {
	if($Server::LSLoading) {
		return;
	}

	if(%brick.numEvents > 0) {
		for(%idx = 0; %idx < %brick.numEvents; %idx++) {
			cancel(%brick.updateDelay["event", %idx]);

			%params = getFields(%brick.serializeEventToString(%idx), 7, 10);

			%data = "brickEvent" TAB
			%brick._LS_uniq TAB
			%idx TAB
			%brick.eventEnabled[%idx] TAB
			%brick.eventInput[%idx] TAB
			%brick.eventDelay[%idx] TAB
			%brick.eventTarget[%idx] TAB
			%brick.eventNT[%idx] TAB
			%brick.eventOutput[%idx] TAB
			%params;

			%brick.updateDelay["event", %idx] = %brick.schedule(5000, pushLSUpdate, %data);
		}
	}
}

// would love lambda functions here but toreque enging
function fxDTSBrick::pushLSUpdate(%this, %data) {
	LiveSaverTCPLines.send(%data);
}

package LSLivePackage {
	function fxDTSBrickData::onColorChange(%db, %brick) {
		%brick.sendLSUpdate("color");
		return parent::onColorChange(%db, %brick);
	}

	function fxDTSBrick::setLight(%this, %light, %client) {
		%this.sendLSUpdate("light", %light);
		return parent::setLight(%this, %light, %client);
	}

	function fxDTSBrick::setEmitter(%this, %emitter, %client) {
		%this.sendLSUpdate("emitter", %emitter);
		return parent::setEmitter(%this, %emitter, %client);
	}

	function fxDTSBrick::setEmitterDirection(%this, %dir) {
		%this.sendLSUpdate("emitterDir", %dir);
		return parent::setEmitterDirection(%this, %dir);
	}

	function fxDTSBrick::setItem(%this, %item, %client) {
		%this.sendLSUpdate("item", %item);
		return parent::setItem(%this, %item, %client);
	}

	function fxDTSBrick::setItemPosition(%this, %pos) {
		%this.sendLSUpdate("itemPos", %pos);
		return parent::setItemPosition(%this, %pos);
	}

	function fxDTSBrick::setItemDirection(%this, %dir) {
		%this.sendLSUpdate("itemDir", %dir);
		return parent::setItemDirection(%this, %dir);
	}

	function fxDTSBrick::setItemRespawnTime(%this, %msec) {
		%this.sendLSUpdate("itemTime", %msec);
		return parent::setItemRespawnTime(%this, %msec);
	}

	function fxDTSBrick::setRendering(%this, %a) {
		%this.sendLSUpdate("attr", %this.isRaycasting() SPC %this.isColliding() SPC %a);
		parent::setRendering(%this, %a);
	}

	function fxDTSBrick::setColliding(%this, %a) {
		%this.sendLSUpdate("attr", %this.isRaycasting() SPC %a SPC %this.isRendering());
		parent::setColliding(%this, %a);
	}

	function fxDTSBrick::setRaycasting(%this, %a) {
		%this.sendLSUpdate("attr", %a SPC %this.isColliding() SPC %this.isRendering());
		parent::setRaycasting(%this, %a);
	}

	function SimObject::setNTObjectName(%this, %name) {
		if(%this._LS_uniq $= "" || %name $= "") {
			return parent::setNTObjectName(%this, %name);
		}

		%sName = %name;
		if(getSubStr(%sName, 0, 1) !$= "_") {
			%sName = "_" @ %name;
		}
		%this.sendLSUpdate("name", %sName);

		return parent::setNTObjectName(%this, %name);
	}

	function fxDTSBrick::setReColorVehicle(%this, %a) {
		%this.sendLSUpdate("colorVehicle", %a);
		parent::setReColorVehicle(%this, %a);
	}

	function fxDTSBrick::setVehicle(%this, %db, %client) {
		%this.sendLSUpdate("vehicle", %db.getName());
		parent::setVehicle(%this, %db, %client);
	}

	function fxDTSBrick::setSound(%this, %db, %client) {
		%this.sendLSUpdate("music", %db.getName());
		parent::setSound(%this, %db, %client);		
	}

	function fxDTSBrick::setPrint(%this, %id) {
		%this.sendLSUpdate("print", %id);
		parent::setPrint(%this, %id);
	}

	function fxDTSBrick::setColorFX(%this, %fx) {
		%this.schedule(66, _LS_delayCheckColorFX);
		parent::setColorFX(%this, %fx);
	}

	function fxDTSBrick::setShapeFX(%this, %fx) {
		%this.sendLSUpdate("shapeFxID", %this.shapeFxID || 0);
		parent::setShapeFX(%this, %fx);
	}

	function fxDTSBrick::_LS_delayCheckColorFX(%this) {
		if($NDHN[%this] $= $oldNDHN[%this]) {
			%this.sendLSUpdate("colorFxID", %this.colorFxID || 0);
		}
		$oldNDHN[%this] = $NDHN[%this];
	}

	function serverCmdAddEvent(%client, %enabled, %inputEventIdx, %delay, %targetIdx, %NTNameIdx, %outputEventIdx, %par1, %par2, %par3, %par4) {
		%a = parent::serverCmdAddEvent(%client, %enabled, %inputEventIdx, %delay, %targetIdx, %NTNameIdx, %outputEventIdx, %par1, %par2, %par3, %par4);
		// need some way to prevent spam
		%client.wrenchBrick.sendLSEventUpdate();
		return %a;
	}

	function SimObject::clearEvents(%this) {
		parent::clearEvents(%this);
		if(%this.getClassName() $= "fxDTSBrick") {
			cancel(%this.updateDelay["eventClear"]);
			%this.updateDelay["eventClear"] = %this.schedule(5000, pushLSUpdate, "eventClear" TAB %this._LS_uniq);
		}
	}
};
activatePackage(LSLivePackage);