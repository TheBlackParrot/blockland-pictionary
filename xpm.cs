if(!isObject(XPMFile)) {
	new FileObject(XPMFile);
}

function testBricks() {
	for(%i=0;%i<$Pictionary::BoardCount;%i++) {
		if(isObject($Pictionary::BoardBrick[%i])) {
			$Pictionary::BoardBrick[%i].schedule(3*%i, setColor, 0);
		}
	}
}

$Pictionary::BoardStartPosition = "139.25 -3.25 29.9";
$Pictionary::BoardWidth = 128;
$Pictionary::BoardHeight = 50;
function generateBoard() {
	%start[x] = getWord($Pictionary::BoardStartPosition, 0);
	%start[y] = getWord($Pictionary::BoardStartPosition, 1);
	%start[z] = getWord($Pictionary::BoardStartPosition, 2);

	%count = 0;
	for(%y=0;%y<$Pictionary::BoardHeight;%y++) {
		for(%x=0;%x<$Pictionary::BoardWidth;%x++) {
			%brick = new fxDTSBrick(_boardBrick) {
				angleID = 1;
				client = 0;
				colorFxID = 3;
				colorID = 63;
				dataBlock = "brick1x1PrintData";
				isBasePlate = 0;
				isPlanted = 1;
				position = %start[x] SPC %start[y] - (%x*0.5) SPC %start[z] - (%y*0.6);
				printID = 60;
				rotation = "0 0 1 90";
				shapeFxID = 0;
				stackBL_ID = -1;
			};
			$Pictionary::BoardBrick[%count] = %brick;
			%brick.plant();
			%brick.setTrusted(1);

			BrickGroup_888888.add(%brick);

			%count++;
		}
	}

	$Pictionary::BoardCount = %count;
}

function saveBoardXPM() {
	if(!$Pref::Pictionary::SaveImage) {
		return;
	}

	%mg = $DefaultMinigame;

	if(%mg.numMembers < 5) {
		messageAll('', "Not enough players to save the XPM image.");
		return;
	}

	if(%mg.stopSave) {
		%mg.stopSave = 0;
		return;
	}

	%start = getRealTime();

	%filename = "config/server/Pictionary/drawings/" @ stripChars(getDateTime(), " :/") @ "_" @ %mg.chosenClient.bl_id @ ".xpm";
	%chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+.";

	XPMFile.openForWrite(%filename);
	XPMFile.writeLine("! XPM2");
	XPMFile.writeLine("! Drawn by" SPC %mg.chosenClient.name SPC "(" @ %mg.chosenClient.bl_id @ ") at" SPC getDateTime());
	XPMFile.writeLine("! Word was" SPC getAltWords(%mg.word, "/"));
	XPMFile.writeLine($Pictionary::BoardWidth SPC $Pictionary::BoardHeight SPC "64 1");

	for(%i=0;%i<strLen(%chars);%i++) {
		XPMFile.writeLine(getSubStr(%chars, %i, 1) SPC "c" SPC "#" @ RGBToHex(getColorIDTable(%i)));
	}

	%count = 0;
	for(%y=0;%y<$Pictionary::BoardHeight;%y++) {
		%str = "";

		for(%x=0;%x<$Pictionary::BoardWidth;%x++) {
			%str = trim(%str @ getSubStr(%chars, $Pictionary::BoardBrick[%count].colorID, 1));
			%count++;
		}

		XPMFile.writeLine(%str);
	}

	XPMFile.close();

	%end = getRealTime();
	messageAll('', "\c0Saved XPM image to" SPC %filename SPC "in" SPC getTimeString((%end-%start)/1000));
	if(getNumKeyID() == 18701) {
		// I'll assume if you're smart enough to set up your own viewer, you'll probably be smart enough to edit this.
		// If anyone /does/ want to make one, I used Git as a buffer with some bash scripts to push every 15 minutes
		// It will also clone on another server every 15+1 minutes.

		// Of course, if you only use 1 server for everything, you don't need to do this lol.
		
		messageAll('', "Images can be viewed <a:blpictionary.theblackparrot.us>here</a>. The page is updated every 15 minutes.");
	}
}