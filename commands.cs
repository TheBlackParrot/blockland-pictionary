function serverCmdSeeWord(%client) {
	if(!%client.isAdmin) {
		return;
	}

	if(%client.pictionaryFloodProtect()) {
		return;
	}

	%client.guessed = 1;
	messageClient(%client, '', "\c2" @ getAltWords($DefaultMinigame.word, "/"));
}

function serverCmdSkip(%client) {
	if(!%client.isAdmin) {
		return;
	}

	if(%client.pictionaryFloodProtect()) {
		return;
	}

	$DefaultMinigame.stopSave = 1;

	$DefaultMinigame.messageAll('', "\c3" @ %client.name SPC "\c5has forcefully skipped this round.");
	
	cancel($DefaultMinigame.endRoundSched);
	$DefaultMinigame.endRound();
}

function serverCmdOverride(%client, %who) {
	if(!%client.isSuperAdmin) {
		return;
	}

	if(%client.pictionaryFloodProtect()) {
		return;
	}

	%target = findClientByName(%who);
	if(!isObject(%target)) {
		%target = findClientByBL_ID(%who);
		if(!isObject(%target)) {
			messageClient(%client, '', %who SPC "does not exist.");
			return;
		}
	}

	$Pictionary::Override = %target;
	$DefaultMinigame.messageAll('', "\c3" @ %target.name SPC "\c5has been manually selected to be the next drawer");
}

function serverCmdLike(%client) {
	if(%client.liked) {
		return;
	}

	if(%client.canDraw) {
		return;
	}

	if(%client == $DefaultMinigame.chosenClient) {
		return;
	}

	if(%client.pictionaryFloodProtect()) {
		return;
	}

	%client.liked = 1;
	$Pictionary::Likes++;

	if(isObject($DefaultMinigame.chosenClient)) {
		messageClient($DefaultMinigame.chosenClient, '', "\c3" @ %client.name SPC "\c5likes your drawing!");
	}

	messageClient(%client, '', "\c2The image has been liked" SPC $Pictionary::Likes SPC "times.");
}

function serverCmdDislike(%client) {
	if(!%client.liked) {
		return;
	}

	if(%client.canDraw) {
		return;
	}

	if(%client.pictionaryFloodProtect()) {
		return;
	}

	%client.liked = 0;
	$Pictionary::Likes--;

	if(isObject($DefaultMinigame.chosenClient)) {
		messageClient($DefaultMinigame.chosenClient, '', "\c3" @ %client.name SPC "\c0took back their like!");
	}

	messageClient(%client, '', "\c0You took back your like. \c2The image has been liked" SPC $Pictionary::Likes SPC "times.");
}
function serverCmdUnlike(%client) {
	serverCmdDislike(%client);
}

function serverCmdQueue(%client) {
	if(%client.canDraw) {
		return;
	}

	if(%client.pictionaryFloodProtect()) {
		return;
	}

	if(%client.isBannedFromDrawing()) {
		messageClient(%client, '', "\c0You are banned from drawing.");
		return;
	}

	for(%i=0;%i<queueGroup.getCount();%i++) {
		%obj = queueGroup.getObject(%i);

		if(%obj.client == %client || %obj.client.bl_id == %client.bl_id) {
			messageClient(%client, '', "\c2You are #" @ %i+1 SPC "in the queue.");
			return;
		}
	}

	%row = new ScriptObject(QueueRow) {
		client = %client;
	};
	queueGroup.add(%row);

	messageClient(%client, '', "\c2You have been added to the queue. You are #" SPC queueGroup.getCount() SPC "in the list.");
}
function serverCmdDraw(%client) {
	serverCmdQueue(%client);
}

function serverCmdUnqueue(%client) {
	if(%client.pictionaryFloodProtect()) {
		return;
	}

	for(%i=0;%i<queueGroup.getCount();%i++) {
		%obj = queueGroup.getObject(%i);

		if(%obj.client == %client) {
			messageClient(%client, '', "\c2You are no longer in the queue.");
			%obj.delete();
			return;
		}
	}

	messageClient(%client, '', "You are not queued, use \c5/queue \c0to join.");
}
function serverCmdLeaveQueue(%client) {
	serverCmdUnqueue(%client);
}

function serverCmdViewQueue(%client) {
	if(%client.pictionaryFloodProtect(2)) {
		return;
	}

	for(%i=0;%i<queueGroup.getCount();%i++) {
		messageClient(%client, '', "\c2" @ %i+1 @ ".\c6" SPC queueGroup.getObject(%i).client.name);
	}
}

function serverCmdAutoQueue(%client) {
	if(%client.pictionaryFloodProtect()) {
		return;
	}

	if(%client.isBannedFromDrawing()) {
		return;
	}

	if(%client.autoQueue) {
		%client.autoQueue = 0;
		messageClient(%client, '', "\c0You are no longer automatically queued.");
		return;
	} else {
		%client.autoQueue = 1;
		messageClient(%client, '', "\c0You are now automatically queued.");
	}

	serverCmdQueue(%client);
}

function serverCmdDrawBan(%client, %who) {
	if(!%client.isAdmin) {
		return;
	}

	if(%client.pictionaryFloodProtect()) {
		return;
	}

	%victim = findClientByName(%who);
	if(!isObject(%victim)) {
		%victim = findClientByBL_ID(%who);
		if(!isObject(%victim) && getSubStr(%who, 0, 3) !$= "@ID") {
			messageClient(%client, '', "This player does not exist.");
			return;
		}
	}

	if(getSubStr(%who, 0, 3) $= "@ID") {
		%bl_id = getSubStr(%who, 3, strLen(%who));
		%name = "N/A";
	} else {
		%bl_id = %victim.bl_id;
		%name = %victim.name;
	}

	if(!isObject(BannedDrawers)) {
		messageClient(%client, '', "ERROR: BannedDrawers script group does not exist.");
		return;
	}

	%found = 0;
	for(%i=0;%i<BannedDrawers.getCount();%i++) {
		%row = BannedDrawers.getObject(%i);
		if(%row.bl_id == %who || stripos(%row.name, %who) != -1 || %bl_id == %row.bl_id) {
			%found = 1;
			break;
		}
	}

	if(%found) {
		messageClient(%client, '', "ERROR: User already banned.");
		return;
	}

	%row = new ScriptObject(BannedDrawer) {
		bl_id = %bl_id;
		name = %name;
		bannedAt = getDateTime();
	};
	BannedDrawers.add(%row);

	$DefaultMinigame.messageAll('MsgAdminForce', "\c3" @ ((%name $= "N/A") ? "\c1BL_ID" SPC %bl_id : %name) SPC "\c5has been banned from drawing");
	
	if(isObject(%victim)) {
		if($DefaultMinigame.chosenClient == %victim) {
			$DefaultMinigame.stopSave = 1;
			$DefaultMinigame.endRound();
		}
		if(%victim.autoQueue) {
			serverCmdUnqueue(%victim);
			%victim.autoQueue = 1;
		}

		for(%i=0;%i<queueGroup.getCount();%i++) {
			%obj = queueGroup.getObject(%i);

			if(%obj.client == %victim || %obj.client.bl_id == %victim.bl_id) {
				%obj.delete();
				%i--;
				return;
			}
		}
	}

	saveBannedDrawers();
}

function serverCmdDrawUnban(%client, %who) {
	if(!%client.isAdmin) {
		return;
	}

	if(%client.pictionaryFloodProtect()) {
		return;
	}

	%found = 0;
	for(%i=0;%i<BannedDrawers.getCount();%i++) {
		%row = BannedDrawers.getObject(%i);
		if(%row.bl_id == %who || stripos(%row.name, %who) != -1) {
			%found = 1;
			break;
		}
	}

	if(!%found) {
		messageClient(%client, '', "ERROR: User not banned.");
		return;
	}

	$DefaultMinigame.messageAll('MsgAdminForce', "\c3" @ %row.name SPC "\c5is no longer banned from drawing");
	%row.delete();

	saveBannedDrawers();
}

function serverCmdPVP(%client) {
	if(%client.pictionaryFloodProtect()) {
		return;
	}

	if(%client.pvp) {
		%client.pvp = 0;
		messageClient(%client, '', "\c0PVP is now disabled.");
		return;
	}

	if(!%client.pvp) {
		%client.pvp = 1;
		messageClient(%client, '', "\c2PVP is now enabled.");
		return;
	}
}

function serverCmdHelp(%client, %page) {
	if(%client.pictionaryFloodProtect()) {
		return;
	}

	messageClient(%client, '', "\c6In \c3Pictionary, \c6the goal is to guess what the selected drawer is attempting to draw. Guessing is as simple as chatting.");
	messageClient(%client, '', "\c6Drawers are selected at random, nothing anyone does boosts or reduces chances of being selected.");
	messageClient(%client, '', "\c4/queue \c7-- \c6Enter the drawing queue.");
	messageClient(%client, '', "\c4/leaveQueue \c7-- \c6Leave the drawing queue.");
	messageClient(%client, '', "\c4/viewQueue \c7-- \c6View the drawing queue.");
	messageClient(%client, '', "\c4/autoQueue \c7-- \c6Toggle automatically being queued.");
	messageClient(%client, '', "\c4/like \c7-- \c6Like the drawing.");
	messageClient(%client, '', "\c4/dislike \c7-- \c6Take back your like.");
	messageClient(%client, '', "\c4/PVP \c7-- \c6Take damage from snowballs. (both players have to have PVP on)");

	if(%client.canDraw || %page $= "drawing") {
		messageClient(%client, '', "\c4To draw, click and hold on the white board in front of you. Your word is in the bottom print. You have 100 seconds to draw.");
		messageClient(%client, '', "\c4To paint, use your paintcan as if you were building.");
		messageClient(%client, '', "\c2" @ "/radius \c5[1-8] \c7-- \c6Change the size of your brush.");
		messageClient(%client, '', "\c2" @ "/shape \c5[square, circle] \c7-- \c6Change the shape of your brush.");
		messageClient(%client, '', "\c2" @ "/erase \c7-- \c6Shortcut to the last color in the colorset.");
		messageClient(%client, '', "\c2" @ "/clear \c5[blank or [color]] \c7-- \c6Clear the board or clear it with a color.");
		messageClient(%client, '', "\c2" @ "/fill \c7-- \c6Resets the board to the color you currently have selected.");
		messageClient(%client, '', "\c2" @ "/pass \c7-- \c6Skip your round, if you don't wish to draw.");
		messageClient(%client, '', "\c5" @ "SINGLE LETTER SHORTCUTS ALSO WORK, e.g. \c6/s c will set your brush shape to a circle.");
	}

	if(%client.isAdmin) {
		messageClient(%client, '', "\c1" @ "/drawBan \c5[player] \c7-- \c6Bans the player from drawing.");
		messageClient(%client, '', "\c1" @ "/drawUnban \c5[player] \c7-- \c6Lets a previously banned player draw again.");
		messageClient(%client, '', "\c1" @ "/seeWord \c7-- \c6See the current word, be aware this nulls your ability to guess for the round.");
		messageClient(%client, '', "\c1" @ "/skip \c7-- \c6Force skips the round.");
	}

	if(%client.isSuperAdmin) {
		messageClient(%client, '', "\c0" @ "/override \c5[player] \c7-- \c6Sets the drawer for the next round.");
	}
}

package AdminChatPackage {
	function serverCmdMessageSent(%client, %msg) {
		if(getSubStr(%msg, 0, 3) $= "!ac") {
			if(!%client.isAdmin) {
				return parent::serverCmdMessageSent(%client, %msg);
			}

			%msg = stripMLControlChars(trim(getSubStr(%msg, 3, strLen(%msg))));
			for(%i=0;%i<ClientGroup.getCount();%i++) {
				%tmp = ClientGroup.getObject(%i);
				if(%tmp.isAdmin) {
					%format = '\c7%1\c3%2\c7%3\c6: %4';
					commandToClient(%tmp, 'chatMessage', %tmp, '', '', %format, "\c1*** ", "\c4" @ %client.name, "", "\c3" @ %msg);
				}
			}

			if(%client.isAdmin) {
				return;
			}
		}

		return parent::serverCmdMessageSent(%client, %msg);
	}
};
if($Pref::Pictionary::EnableAdminChat) {
	activatePackage(AdminChatPackage);
}