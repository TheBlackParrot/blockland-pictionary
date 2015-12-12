if(!isObject(queueGroup)) {
	new ScriptGroup(queueGroup);
}

function GameConnection::isBannedFromDrawing(%this) {
	for(%i=0;%i<BannedDrawers.getCount();%i++) {
		%row = BannedDrawers.getObject(%i);
		if(%this.bl_id == %row.bl_id) {
			return 1;
		}
	}
	return 0;
}

function MinigameSO::selectDrawer(%this) {
	if(%this.numMembers < 1) {
		return;
	}

	while(%chosen.isBannedFromDrawing() || %chosen $= "") {
		if(isObject($Pictionary::Override)) {
			%chosen = $Pictionary::Override;
			$Pictionary::Override = "";
		} else {
			if(queueGroup.getCount() <= 0) {
				%max = %this.numMembers - 1;
				
				%chosen = getRandom(0, %max);
				if(%this.numMembers > 1) {
					%attempts = 0;
					while(%this.member[%chosen] == %this.lastChosen || %this.member[%chosen].isBannedFromDrawing() && %attempts < 1000) {
						%chosen = getRandom(0, %max);
						%attempts++; // preventing really cool people
					}
				}
				%chosen = %this.member[%chosen];
				%this.lastChosen = %chosen;
			} else {
				%chosen = queueGroup.getObject(0).client;
				queueGroup.getObject(0).delete();
				queueGroup.pushToBack(queueGroup.getObject(0));
			}
		}
	}

	%chosen.canDraw = 1;
	if(!isObject(%chosen.player)) {
		%chosen.spawnPlayer();
	}
	%player = %chosen.player;
	%this.chosenClient = %chosen;

	%player.setTransform(_chosenSpawn.getSpawnPoint());
	%player.drawingInfoLoop();
	%this.messageAll('MsgAdminForce', "\c3" @ %chosen.name SPC "\c5is the drawer this round!");
	%player.client.play2D(errorSound);
	%player.client.schedule(200, play2D, errorSound);
	%player.client.schedule(400, play2D, errorSound);
	messageClient(%chosen, '', "\c2Admins are able to see your chat messages. \c4Use /help for further help.");
	%player.client.afkSched = %player.client.schedule(30000, endForAFK);

	for(%i=0;%i<%this.numMembers;%i++) {
		%client = %this.member[%i];
		%client.guessed = 0;

		if(%client != %chosen) {
			%client.canDraw = 0;
			cancel(%client.afkSched);
		}
	}
	$Pictionary::Guessed = 0;

	for(%i=0;%i<queueGroup.getCount();%i++) {
		%row = queueGroup.getObject(%i);
		if(isObject(%row.client)) {
			messageClient(%row.client, '', "\c2You are now in position #" @ %i+1 SPC "in the queue.");
		}
	}

	$DefaultMinigame.stopSave = 0;
}

function MinigameSO::checkGuessed(%this) {
	for(%i=0;%i<%this.numMembers;%i++) {
		%client = %this.member[%i];
		if(%client.guessed) {
			%count++;
		}
	}

	if(%count == %this.numMembers-1) {
		cancel(%this.endRoundSched);
		%this.endRound();
	}
}

function MinigameSO::updateWord(%this) {
	%this.word = getPictionaryWord();
	%this.roundID = sha1(getDateTime()); // for later

	$Pictionary::CanGuess = 1;
}

function clearBoard() {
	for(%i=0;%i<$Pictionary::BoardCount;%i++) {
		if(isObject($Pictionary::BoardBrick[%i])) {
			$Pictionary::BoardBrick[%i].setColor(63);
		}
	}
}

function MinigameSO::startRound(%this) {
	if(isEventPending(%this.endRoundSched)) {
		return;
	}

	%this.messageAll('', "\c5The drawing was liked\c3" SPC $Pictionary::Likes SPC "times.");
	if($Pictionary::Likes/%this.numMembers >= 0.15 && $Pictionary::Likes >= 3) {
		saveBoardXPM();
	}
	$Pictionary::Likes = 0;

	%this.chosenClient = "";

	schedule(100, 0, clearBoard);

	if(%this.numMembers < 1) {
		%this.messageAll('', "\c5No one is around! Spooky. Waiting 10 more seconds...");
		%this.schedule(10000, startRound);
		return;
	}

	for(%i=0;%i<%this.numMembers;%i++) {
		%client = %this.member[%i];
		%client.liked = 0;

		%client.saveScore();
	}

	%this.messageAll('', "\c5Choosing the next drawer...");

	%time = $Pref::Pictionary::RoundTime;
	_time1.setPrintCount(%time >= 100 ? getSubStr(%time, 2, 1) : getSubStr(%time, 1, 1));
	_time2.setPrintCount(%time >= 100 ? getSubStr(%time, 1, 1) : getSubStr(%time, 0, 1));
	_time3.setPrintCount(%time >= 100 ? getSubStr(%time, 0, 1) : 0);

	%this.schedule(2999, updateWord);
	%this.schedule(3000, selectDrawer);
	%this.schedule(3000, doTimer);

	changePictionaryMusic();

	%this.endRoundSched = %this.schedule((%time*1000) + 3000, endRound);
	%this.startedAt = $Sim::Time;
	%this.endingAt = $Sim::Time + %time + 3;
}

function MinigameSO::endRound(%this) {
	%this.messageAll('', "\c5Good game! The word was\c3" SPC getAltWords(%this.word, "/") @ ".");
	$Pictionary::CanGuess = 0;

	if(isObject(%this.chosenClient)) {
		%this.chosenClient.canDraw = 0;

		if(isObject(%this.chosenClient.player)) {
			%this.chosenClient.instantRespawn();
		} else {
			%this.chosenClient.spawnPlayer();
		}

		if(%this.chosenClient.autoQueue) {
			serverCmdQueue(%this.chosenClient);
		}
	}

	cancel(%this.timerLoop);
	_time1.setPrintCount(0);
	_time2.setPrintCount(0);
	_time3.setPrintCount(0);

	%this.schedule(8000, startRound);
}

function GameConnection::endForAFK(%this) {
	$DefaultMinigame.messageAll('', "\c3" @ %this.name SPC "\c5was AFK");

	cancel($DefaultMinigame.endRoundSched);
	$DefaultMinigame.endRound();

	if(%client.autoQueue) {
		%client.autoQueue = 0;
		messageClient(%client, '', "\c0You are no longer autoqueued due to being AFK.");
	}

	//%this.delete("AFK while drawing");
}

function MinigameSO::doTimer(%this) {
	cancel(%this.timerLoop);
	%this.timerLoop = %this.schedule(1000, doTimer);

	_time1.fireRelay();
}