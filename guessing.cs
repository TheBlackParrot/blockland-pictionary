function GameConnection::guessWord(%this, %guess) {
	%guess = strLwr(stripChars(%guess, " !@#$%^&*()_+-={}|[]\\:\";',./<>?~`0123456789"));
	%correctAll = strLwr(strReplace($DefaultMinigame.word, " ", ""));
	%mg = $DefaultMinigame;

	for(%i=0;%i<getFieldCount(%correctAll);%i++) {
		%correct = getField(%correctAll, %i);
		if(stripos(%guess, %correct) != -1) {
			if(!%this.guessed) {
				%this.guessed = 1;
				messageClient(%this, '', "\c5CORRECT! The word is \c3" @ getAltWords(%mg.word, "/"));
				%this.play2D(errorSound);
				messageAll('', "\c3" @ %this.name SPC "\c5guessed the word");
				%this.score++;

				%mg.checkGuessed();

				$Pictionary::Guessed++;
				if($Pictionary::Guessed/%mg.numMembers > 0.75) {
					if(%mg.endingAt - $Sim::Time > 50) {
						cancel(%mg.endRoundSched);

						%mg.endRoundSched = %mg.schedule(50000, endRound);
						%mg.endingAt = $Sim::Time + 50;
					}
				}
			}
			return 1;
		}
	}

	return 0;
}

function GameConnection::spamLoop(%this) {
	cancel(%this.spamSched);
	%this.spamSched = %this.schedule(2000, spamLoop);

	%this.msgCount--;
	if(%this.msgCount < 0) {
		%this.msgCount = 0;
	}
}

package PictionaryGuessingPackage {
	function serverCmdMessageSent(%client, %msg) {
		if(!$Pictionary::CanGuess) {
			return parent::serverCmdMessageSent(%client, %msg);
		}

		if(%client.guessed && !%client.isAdmin) {
			%format = '\c7%1\c3%2\c7%3\c6: %4';

			if(%client.lastMsg $= %msg) {
				return;
			}
			%client.lastMsg = %msg;

			%client.msgCount++;
			if(%client.msgCount > 3) {
				return;
			}

			for(%i=0;%i<ClientGroup.getCount();%i++) {
				%tmp = ClientGroup.getObject(%i);
				if(%tmp.guessed || %tmp.canDraw || %tmp.isAdmin) {
					commandToClient(%tmp, 'chatMessage', %tmp, '', '', %format, "\c0[GUESSED]\c7" SPC %client.clanPrefix, %client.name, %client.clanSuffix, "<color:cccccc>" @ %msg);
				}
			}
			return;
		}

		if(%client.isAdmin && %client.guessWord(%msg)) {
			return;
		}

		if(%client.isAdmin && !%client.guessWord(%msg)) {
			return parent::serverCmdMessageSent(%client, %msg);
		}
		
		if(!isEventPending(%client.spamSched)) {
			%client.spamLoop();
		}

		%msg = stripMLControlChars(%msg);

		if(%client.canDraw) {
			//messageClient(%client, '', "\c4[DRAWER] \c3" @ %client.name @ "\c6: " @ %msg);
			%format = '\c7%1\c3%2\c7%3\c6: %4';
			for(%i=0;%i<ClientGroup.getCount();%i++) {
				%tmp = ClientGroup.getObject(%i);
				if(%tmp.isAdmin || %tmp.guessed || %tmp == %client) {
					// for now
					commandToClient(%tmp, 'chatMessage', %tmp, '', '', %format, "\c4[DRAWER]" SPC %client.clanPrefix, %client.name, %client.clanSuffix, %msg);
				}
			}
			return;
		}

		if(!%client.guessWord(%msg)) {
			return parent::serverCmdMessageSent(%client, %msg);
		}
	}

	function serverCmdTeamMessageSent(%client, %msg) {
		return;
	}
};
activatePackage(PictionaryGuessingPackage);