function serverCmdRadius(%client, %radius) {
	if(%client.pictionaryFloodProtect()) {
		return;
	}

	%radius = mClamp(%radius, 1, 8);
	%client.radius = %radius;

	messageClient(%client, '', "\c5Paint radius set to\c3" SPC %radius);
}
function serverCmdR(%client, %radius) {
	serverCmdRadius(%client, %radius);
}
function serverCmdSize(%client, %radius) {
	serverCmdRadius(%client, %radius);
}

function serverCmdR1(%client) { serverCmdRadius(%client, 1); }
function serverCmdR2(%client) { serverCmdRadius(%client, 2); }
function serverCmdR3(%client) { serverCmdRadius(%client, 3); }
function serverCmdR4(%client) { serverCmdRadius(%client, 4); }
function serverCmdR5(%client) { serverCmdRadius(%client, 5); }
function serverCmdR6(%client) { serverCmdRadius(%client, 6); }
function serverCmdR7(%client) { serverCmdRadius(%client, 7); }
function serverCmdR8(%client) { serverCmdRadius(%client, 8); }

function serverCmdClear(%client, %color) {
	if(!%client.canDraw) {
		return;
	}
	
	if(%client.pictionaryFloodProtect(3)) {
		return;
	}

	if($DefaultMinigame.endingAt - $Sim::Time < 15) {
		return;
	}

	cancel(%client.afkSched);
	
	if(%color !$= "") {
		switch$(%color) {
			case "red": %color = 1;
			case "orange": %color = 10;
			case "yellow": %color = 14;
			case "green": %color = 22;
			case "cyan" or "turquoise" or "aqua": %color = 30;
			case "blue" or "indigo": %color = 32;
			case "pink" or "fuschia": %color = 46;
			case "white": %color = 55;
			case "black": %color = 48;
			case "grey" or "gray" or "silver": %color = 51;
			case "brown": %color = 57;
		}
	} else {
		%color = 54;
	}
	%color = mClamp(%color, 0, 63);

	for(%i=0;%i<$Pictionary::BoardCount;%i++) {
		if(isObject($Pictionary::BoardBrick[%i])) {
			$Pictionary::BoardBrick[%i].setColor(%color);
		}
	}

	messageClient(%client, '', "\c5Board cleared");
}
function serverCmdC(%client, %color) {
	serverCmdClear(%client, %color);
}

function serverCmdFill(%client) {
	if(!isObject(%client.player)) {
		return;
	}

	serverCmdClear(%client, %client.player.currSprayCan);
}
function serverCmdF(%client) {
	serverCmdFill(%client);
}

function serverCmdShape(%client, %shape) {
	if(%client.pictionaryFloodProtect()) {
		return;
	}
	
	switch$(%shape) {
		case 0 or "r" or "round" or "c" or "circle" or "circular": %shape = "circle";
		case 1 or "re" or "rect" or "box" or "b" or "s" or "square" or "sq" or "rectangle" or "sharp" or "rectangular": %shape = "square";
	}
	if(%shape !$= "circle" && %shape !$= "square") {
		return;
	}

	%client.shape = %shape;
	messageClient(%client, '', "\c5Brush shape set to\c3" SPC %shape);
}
function serverCmdS(%client, %shape) {
	serverCmdShape(%client, %shape);
}

function serverCmdErase(%client) {
	if(!%client.canDraw) {
		return;
	}

	if(!isObject(%client.player)) {
		return;
	}

	%client.player.currSprayCan = 63;
	messageClient(%client, '', "\c5You are now erasing.");
}
function serverCmdE(%client) {
	serverCmdErase(%client);
}

function serverCmdPass(%client) {
	if(!%client.canDraw) {
		return;
	}

	if(%client.pictionaryFloodProtect()) {
		return;
	}

	messageAll('MsgAdminForce', "\c5The drawer decided to pass.");
	cancel($DefaultMinigame.endRoundSched);
	$DefaultMinigame.endRound();
}

function Player::drawPixel(%this) {
	%client = %this.client;
	if(!%client.canDraw) {
		return;
	}

	%eye = vectorScale(%this.getEyeVector(), 100);
	%pos = %this.getEyePoint();
	%mask = $TypeMasks::FXBrickObjectType;
	%hit = getWord(containerRaycast(%pos, vectorAdd(%pos, %eye), %mask, %this), 0);

	if(isObject(%hit)) {
		cancel(%this.client.afkSched);

		if(%client.radius > 1) {
			%pos = %hit.getPosition();
			%radius = (%client.radius $= "") ? 1 : %client.radius-1;
			
			if(%client.shape $= "circle" || %client.shape $= "square") {
				%shape = %client.shape;
			} else {
				%shape = "circle";
			}

			if(%shape $= "square") {
				initContainerBoxSearch(%pos, %radius SPC %radius SPC %radius, %mask);
			} else {
				initContainerRadiusSearch(%pos, %radius, %mask);
			}

			while(%object = containerSearchNext()) {
				if(%object.getName() $= "_boardBrick") {
					%object.setColor(%this.currSprayCan);
				}
			}
		} else {
			if(%hit.getName() $= "_boardBrick") {
				%hit.setColor(%this.currSprayCan);
			}
		}
	}
}

package PictionaryDrawingPackage {
	function Armor::onTrigger(%this, %player, %slot, %val) {
		// redo later
		if(%player.client.canDraw) {
			if(%slot == 0) {
				if(%val) {
					%player.drawLoop();
				} else {
					cancel(%player.drawSched);
				}
			}
		}
		return parent::onTrigger(%this, %player, %slot, %val);
	}
	function Player::drawLoop(%this) {
		cancel(%this.drawSched);
		%this.drawSched = %this.schedule(1, drawLoop);

		%this.drawPixel();
	}
};
activatePackage(PictionaryDrawingPackage);