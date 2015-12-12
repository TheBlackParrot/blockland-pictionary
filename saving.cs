$Pictionary::SaveDir = "config/server/Pictionary/saves";

function GameConnection::saveScore(%this) {
	%file = new FileObject();
	%file.openForWrite($Pictionary::SaveDir @ "/" @ %this.bl_id);

	%file.writeLine(%this.score);

	%file.close();
	%file.delete();
}

function GameConnection::loadScore(%this) {
	%filename = $Pictionary::SaveDir @ "/" @ %this.bl_id;
	if(!isFile(%filename)) {
		return;
	}

	%file = new FileObject();
	%file.openForRead(%filename);

	%this.score = %file.readLine();

	%file.close();
	%file.delete();
}