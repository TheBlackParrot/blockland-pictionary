function gatherTipLines() {
	%file = new FileObject();

	%filename = "config/server/Pictionary/tips.txt";
	if(!isFile(%filename)) {
		%filename = "Add-Ons/Gamemode_Pictionary/tips.txt";
	}

	%file.openForRead(%filename);

	%count = 0;
	while(!%file.isEOF()) {
		$Pictionary::Tip[%count] = %file.readLine();
		%count++;
	}
	$Pictionary::TipCount = %count;
}
if(!$Pictionary::InitTips) {
	$Pictionary::InitTips = 1;
	gatherTipLines();
}

function tipLoop() {
	cancel($Pictionary::TipSched);
	$Pictionary::TipSched = schedule(45000, 0, tipLoop);

	$Pictionary::CurrentTip++;
	if($Pictionary::CurrentTip == $Pictionary::TipCount) {
		$Pictionary::CurrentTip = 0;
	}

	%line = $Pictionary::Tip[$Pictionary::CurrentTip];

	if(getWord(%line, 0) $= "::IFSAVE::") {
		if($Pref::Pictionary::SaveImage) {
			%line = getWords(%line, 1);
		} else {
			cancel($Pictionary::TipSched);
			tipLoop();
			return;
		}
	}
	%line = strReplace(%line, "::WORDCOUNT::", $Pictionary::WordCount);
	%line = strReplace(%line, "::ROUNDTIME::", $Pref::Pictionary::RoundTime);
	%line = strReplace(%line, "::AFKTIME::", $Pref::Pictionary::AFKSkipTime);

	messageAll('', "\c1TIP:\c6" SPC %line);
}
tipLoop();