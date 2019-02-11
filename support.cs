function _LSgetPrintName(%id) {
	// https://forum.blockland.us/index.php?topic=186049.msg4918316#msg4918316
	%texture = getPrintTexture(%id);
	%package = getField(strReplace(%texture, "/", "\t"), 1);
	%category = getField(strReplace(%package, "_", "\t"), 1);
	%name = fileBase(%texture);
	%return = %category @ "/" @ %name;
	return %return;
}