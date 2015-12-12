function GameConnection::showDrawingInfo(%this, %time) {
	%this.bottomPrint("<font:Arial Bold:24>\c3 Word: <font:Arial Black:30>\c6" @ getAltWords($DefaultMinigame.word, "/") @ "<just:right><font:Arial Bold:56>\c2" @ %time @ " <br><just:left><font:Arial Bold:14> \c2Commands: \c6/radius \c7[1-8]    \c6/clear \c7[color or blank]    \c6/shape \c7[circle, square]    \c6/pass", 3, 1);
}

function Player::drawingInfoLoop(%this) {
	// this is intentionally not parented to GameConnection
	cancel(%this.infoSched);
	%this.infoSched = %this.schedule(1000, drawingInfoLoop);

	if(isEventPending($DefaultMinigame.endRoundSched)) {
		%this.client.showDrawingInfo(mFloor($DefaultMinigame.endingAt - $Sim::Time));
	} else {
		%this.client.showDrawingInfo(0);
	}
}