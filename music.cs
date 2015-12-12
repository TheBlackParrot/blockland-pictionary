function initPictionaryMusic() {
	%path = "Add-Ons/Music/Pictionary/";
	%file_s = findFirstFile(%path @ "*.ogg");
	$Pictionary::MusicDataCount = 0;
	echo(%file_s);

	while(%file_s !$= "") {
		%str = "musicData_" @ fileBase(%file_s);
		// get rid of default music
		%blacklist = "After_School_Special.ogg Ambient_Deep.ogg Bass_1.ogg Bass_2.ogg Bass_3.ogg Creepy.ogg Distort.ogg Drums.ogg Factory.ogg Icy.ogg Jungle.ogg Paprika_-_Byakko_no.ogg Peaceful.ogg Piano_Bass.ogg Rock.ogg Stress_.ogg Vartan_-_Death.ogg";
		if(stripos(%blacklist,fileName(%file_s)) == -1) {
			// ugh, i hate using eval
			eval("datablock AudioProfile(musicData_P" @ $Pictionary::MusicDataCount @ ") {fileName = \"" @ %file_s @ "\"; description = \"AudioMusicLooping3d\"; preload = 1; uiName = \"" @ strReplace(fileBase(%file_s),"_"," ") @ "\";};");
			$Pictionary::MusicData[$Pictionary::MusicDataCount] = %str;
			$Pictionary::MusicDataCount++;
		} else {
			warn("SKIPPED" SPC fileName(%file_s) @ ", considering a default loop.");
		}
		%file_s = findNextFile(%path @ "*.ogg");
	}

	$Pictionary::InitMusic = 1;
}
if(!$Pictionary::InitMusic) {
	initPictionaryMusic();
}

function changePictionaryMusic() {
	if(isObject($Pictionary::Music)) {
		$Pictionary::Music.delete();
	}
	$Pictionary::Music = new AudioEmitter() {
		is3D = 0;
		profile = "musicData_P" @ getRandom(0, $Pictionary::MusicDataCount-1);
		referenceDistance = 999999;
		maxDistance = 999999;
		volume = 0.9;
		position = $loadOffset;
	};
}