function fxDTSBrick::_LS_sendBrickToSave(%brick) {
	if(%brick._LS_uniq $= "") {
		%brick._LS_uniq = $Server::LSHighestUniq++;
	}
	$Server::LiveSaver[%brick._LS_uniq] = %brick;

	LiveSaverTCPLines.send(
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
		(%brick.getDatablock().hasPrint ? _LSgetPrintName(%brick.printID) : "") TAB
		(isObject(%brick.light) ? %brick.light.getDatablock().getName() : "") TAB
		(isObject(%brick.AudioEmitter) ? %brick.AudioEmitter.getProfileID().getName() : "") TAB
		%brick.isRaycasting() SPC %brick.isColliding() SPC %brick.isRendering()
	);

	if(isObject(%brick.item)) {
		LiveSaverTCPLines.send(
			"brickItem" TAB
			%brick._LS_uniq TAB
			%brick.item.getDatablock().getName() TAB
			%brick.itemDirection TAB
			%brick.itemPosition TAB
			%brick.itemRespawnTime
		);
	}

	if(isObject(%brick.emitter)) {
		LiveSaverTCPLines.send(
			"brickEmitter" TAB
			%brick._LS_uniq TAB
			%brick.emitter.emitter TAB
			%brick.emitterDirection
		);
	}

	if(isObject(%brick.vehicleDataBlock)) {
		LiveSaverTCPLines.send(
			"brickVehicle" TAB
			%brick._LS_uniq TAB
			%brick.vehicleDataBlock.getName() TAB
			%brick.reColorVehicle
		);
	}

	if(%brick.numEvents > 0) {
		for(%idx = 0; %idx < %brick.numEvents; %idx++) {
			%params = getFields(%brick.serializeEventToString(%idx), 7, 10);

			LiveSaverTCPLines.send(
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
			);
		}
	}

	%brick.isACTUALLYPlanted = true;
}

function _LS_saveAllBricks(%this) {
	for(%i = 0; %i < mainBrickGroup.getCount(); %i++) {
		%group = mainBrickGroup.getObject(%i);

		for(%j = 0; %j < %group.getCount(); %j++) {
			%brick = %group.getObject(%j);
			%brick._LS_sendBrickToSave();
		}
	}
}

package LiveSaverPackage {
	function serverCmdPlantBrick(%client) {
		if($Server::LSLoading) {
			%client.chatMessage("Loading bricks, please wait...");
			return;
		}
		return parent::serverCmdPlantBrick(%client);
	}

	function fxDTSBrick::onAdd(%brick) {
		%a = parent::onAdd(%brick);
		if(!%brick.isPlanted || $Server::LSLoading || %a || %brick.player.tempbrick == %brick) {
			return %a;
		}

		if(%brick._LS_uniq $= "") {
			$Server::LSHighestUniq = %brick._LS_uniq = Math_Add($Server::LSHighestUniq, 1);
		} else {
			if(%brick._LS_uniq > $Server::LSHighestUniq) {
				$Server::LSHighestUniq = %brick._LS_uniq;
			}
		}
		$Server::LiveSaver[%brick._LS_uniq] = %brick;

		%brick.schedule(5000, _LS_sendBrickToSave);

		return %a;
	}

	function fxDTSBrick::onRemove(%brick) {
		if(!%brick.isPlanted || $Server::LSLoading || %a || %brick.player.tempbrick == %brick || %brick._LS_uniq $= "" || !%brick.isACTUALLYPlanted) {
			parent::onRemove(%brick);
			return;
		}

		LiveSaverTCPLines.send("delete" TAB %brick._LS_uniq);

		parent::onRemove(%brick);
	}

	function serverCmdClearAllBricks(%client) {
		if(%client.isAdmin && getBrickcount() > 0) {
			LiveSaverTCPLines.send("save");
			LiveSaverTCPLines.send("clear");
		}

		return parent::serverCmdClearAllBricks(%client);
	}

	function serverCmdClearSpamBricks(%client) {
		if(%client.isAdmin && getBrickcount() > 0) {
			LiveSaverTCPLines.send("save");
		}

		return parent::serverCmdClearSpamBricks(%client);
	}

	function serverCmdClearFloatingBricks(%client) {
		if(%client.isAdmin && getBrickcount() > 0) {
			LiveSaverTCPLines.send("save");
		}

		return parent::serverCmdClearFloatingBricks(%client);
	}

	function onMissionEnded() {
		LiveSaverTCPLines.send("save");
		return parent::onMissionEnded();
	}
};
activatePackage(LiveSaverPackage);