$Pictionary::Root = "Add-Ons/Gamemode_Pictionary";

if(!$Pictionary::Init) {
	$Pictionary::Init = 1;
	$Pictionary::GeneratedBoard = 0;
	$Pictionary::BoardCount = 0;
	$Pictionary::SpectatorSpawnCount = 0;
}

exec("./support.cs");
exec("./db.cs");
exec("./player.cs");
exec("./drawing.cs");
exec("./system.cs");
exec("./guessing.cs");
exec("./music.cs");
exec("./commands.cs");
exec("./tips.cs");
exec("./saving.cs");
exec("./xpm.cs");

$Pictionary::Version = "1.0.3-6";
talk("Executed Pictionary v" @ $Pictionary::Version);

if(isFile("config/server/Pictionary/prefs.cs")) {
	$Pref::Pictionary::MusicBlacklist = "";

	exec("config/server/Pictionary/prefs.cs");

	$Pref::Pictionary::SaveImage = mFloor(mClamp($Pref::Pictionary::SaveImage, 0, 1));
	$Pref::Pictionary::RoundTime = mFloor(mClamp($Pref::Pictionary::RoundTime, 15, 600));
	$Pref::Pictionary::AFKSkipTime = mFloor(mClamp($Pref::Pictionary::AFKSkipTime, 5, $Pref::Pictionary::RoundTime - 10));
	$Pref::Pictionary::AllowSprayCan = mFloor(mClamp($Pref::Pictionary::AllowSprayCan, 0, 1));
	$Pref::Pictionary::EnableAdminChat = mFloor(mClamp($Pref::Pictionary::EnableAdminChat, 0, 1));
} else {
	$Pref::Pictionary::SaveImage = 0;
	$Pref::Pictionary::RoundTime = 120;
	$Pref::Pictionary::AFKSkipTime = 30;
	$Pref::Pictionary::AllowSprayCan = 0;
	$Pref::Pictionary::EnableAdminChat = 1;
	$Pref::Pictionary::MusicBlacklist = "After_School_Special.ogg Ambient_Deep.ogg Bass_1.ogg Bass_2.ogg Bass_3.ogg Creepy.ogg Distort.ogg Drums.ogg Factory.ogg Icy.ogg Jungle.ogg Paprika_-_Byakko_no.ogg Peaceful.ogg Piano_Bass.ogg Rock.ogg Stress_.ogg Vartan_-_Death.ogg";

	export("$Pref::Pictionary*", "config/server/Pictionary/prefs.cs");
}

function GameConnection::pictionaryFloodProtect(%client, %time) {
	if(%time $= "") {
		%time = 0.2;
	}

	if($Sim::Time - %this.pfp < %time) {
		return 1;
	}

	%this.pfp = $Sim::Time;

	return 0;
}

function fxDTSBrick::checkGameBrick(%this) {
	if(%this.getName() $= "_boardBrick") {
		$Pictionary::BoardBrick[$Pictionary::BoardCount] = %this;
		$Pictionary::BoardCount++;
	}

	if(%this.getName() $= "_spectatorSpawn") {
		$Pictionary::SpectatorSpawn[$Pictionary::SpectatorSpawnCount] = %this;
		$Pictionary::SpectatorSpawnCount++;
	}
}

function startGame() {
	$DefaultMinigame.startRound();
}

package PictionarySupportPackage {
	function fxDTSBrick::onLoadPlant(%this) {
		parent::onLoadPlant(%this);

		// Blockland apparently doesn't set names immediately?
		%this.schedule(1, checkGameBrick);
	}

	function GameConnection::spawnPlayer(%this) {
		parent::spawnPlayer(%this);

		if(!%this.initialSpawn) {
			%this.initialSpawn = 1;
			%this.loadScore();
		}

		if(%this.canDraw) {
			%this.player.setTransform(_chosenSpawn.getPosition());
		} else {
			%this.player.setTransform($Pictionary::SpectatorSpawn[getRandom(0, $Pictionary::SpectatorSpawnCount - 1)].getPosition());
		}
	}

	function GameConnection::autoAdminCheck(%this) {
		%this.loadScore();
		%this.initialSpawn = 0;

		messageClient(%this, '', "\c4Please see \c5/help \c4for additional info");
		messageClient(%this, '', "\c5PICTIONARY v" @ $Pictionary::Version SPC "created by TheBlackParrot (18701)");
		return parent::autoAdminCheck(%this);
	}

	function GameConnection::onClientLeaveGame(%this) {
		if(%this.canDraw) {
			serverCmdPass(%this);
		}

		for(%i=0;%i<queueGroup.getCount();%i++) {
			%obj = queueGroup.getObject(%i);

			if(%obj.client == %this) {
				%obj.delete();
				break;
			}
		}

		%this.saveScore();

		return parent::onClientLeaveGame(%this);
	}

	function serverCmdUseSprayCan(%client, %color) {
		if($Pref::Pictionary::AllowSprayCan || !isObject(%client.minigame)) {
			return parent::serverCmdUseSprayCan(%client, %color);
		}
		if(isObject(%client.player)) {
			%player = %client.player;
			%player.currSprayCan = mClamp(%color, 0, 63);
		}
	}

	function serverCmdUseFXCan(%client, %which) {
		if(%client.canDraw) {
			%player = %client.player;
			%player.currSprayCan = 63;
		} else {
			return parent::serverCmdUseFXCan(%client, %which);
		}
	}

	function ServerLoadSaveFile_End() {
		parent::ServerLoadSaveFile_End();

		startGame();
		if(!$Pictionary::GeneratedBoard) {
			generateBoard();
			$Pictionary::GeneratedBoard = 1;
		}
	}

	function onServerDestroyed() {
		export("$Pref::Pictionary*", "config/server/Pictionary/prefs.cs");
		deleteVariables("$Pictionary*");
		return parent::onServerDestroyed();
	}

	function tacklePlayer( %obj, %col ) {
		return;
	}

	function Player::activateStuff(%this) {
		%eye = vectorScale(%this.getEyeVector(), 100);
		%pos = %this.getEyePoint();
		%mask = $TypeMasks::FXBrickObjectType;
		%hit = getWord(containerRaycast(%pos, vectorAdd(%pos, %eye), %mask, %this), 0);

		if(isObject(%hit)) {
			if(%hit.getName() $= "_floorClick") {
				%this.setTransform(_floorSpawn.getSpawnPoint());
			}
		}

		return parent::activateStuff(%this);
	}

	function serverCmdDuplicator(%client) {
		if(!%client.isAdmin) {
			return;
		}
		return parent::serverCmdDuplicator(%client);
	}
	function serverCmdDup(%client) {
		if(!%client.isAdmin) {
			return;
		}
		return parent::serverCmdDup(%client);
	}

	function blankaBallProjectile::onCollision(%this, %obj, %col, %fade, %pos, %normal) {
		if(%col.getClassName() !$= "Player") {
			return parent::onCollision(%this, %obj, %col, %fade, %pos, %normal);
		}

		%client = %obj.client;
		%victim = %col.client;

		if(!%client.pvp || !%victim.pvp || %client.canDraw || %victim.canDraw) {
			return parent::onCollision(%this, %obj, %col, %fade, %pos, %normal);
		}

		%col.addHealth(-20);

		return parent::onCollision(%this, %obj, %col, %fade, %pos, %normal);
	}
};
activatePackage(PictionarySupportPackage);