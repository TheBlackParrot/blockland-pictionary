if(!isObject(BannedDrawers)) {
	new ScriptGroup(BannedDrawers);
}

function gatherWordList() {
	%file = new FileObject();
	
	%filename = "config/server/Pictionary/wordlist.txt";
	if(!isFile(%filename)) {
		%filename = $Pictionary::Root @ "/wordlist.txt";
	}

	%file.openForRead(%filename);

	%count = 0;
	while(!%file.isEOF()) {
		$Pictionary::Word[%count] = %file.readLine();
		%count++;
	}
	$Pictionary::WordCount = %count;

	%file.close();
	%file.delete();

	talk("Gathered" SPC %count SPC "words");
}
if(!$Pictionary::GatheredWords) {
	$Pictionary::GatheredWords = 1;
	gatherWordList();
}

function getPictionaryWord() {
	%max = $Pictionary::WordCount-1;
	%tracked = mFloor($Pictionary::WordCount/2);

	%random = getRandom(0, %max);

	for(%i=0;%i<%tracked;%i++) {
		%all = %all @ ":" @ $Pictionary::PreviousWord[%i] @ ":";
		%new[%i+1] = $Pictionary::PreviousWord[%i];
	}

	%random = getRandom(0, %max);
	while(stripos(%all, ":" @ %random @ ":") != -1) {
		%random = getRandom(0, %max);
	}

	%new[0] = %random;

	for(%i=0;%i<%tracked;%i++) {
		$Pictionary::PreviousWord[%i] = %new[%i];
	}

	return $Pictionary::Word[%random];
}

function getAltWords(%str, %replace) {
	return strReplace(%str, "\t", %replace);
}

function getBannedDrawers() {
	%file = new FileObject();
	%file.openForRead("config/server/Pictionary/bans.txt");

	%count = 0;
	while(!%file.isEOF()) {
		%data = %file.readLine();

		%row = new ScriptObject(BannedDrawer) {
			bl_id = getField(%data, 0);
			name = getField(%data, 1);
			bannedAt = getField(%data, 2);
		};
		BannedDrawers.add(%row);

		%count++;
	}

	%file.close();
	%file.delete();

	talk("Gathered" SPC %count SPC "bans");
}
if(!$Pictionary::InitBans) {
	$Pictionary::InitBans = 1;
	getBannedDrawers();
}

function saveBannedDrawers() {
	%file = new FileObject();
	%file.openForWrite("config/server/Pictionary/bans.txt");

	for(%i=0;%i<BannedDrawers.getCount();%i++) {
		%row = BannedDrawers.getObject(%i);
		%file.writeLine(%row.bl_id TAB %row.name TAB %row.bannedAt);
	}

	%file.close();
	%file.delete();
}