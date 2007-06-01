all:
	mcs -out:denise.exe -pkg:gtk-sharp-2.0 -pkg:gconf-sharp-2.0 -r:Gnome.Keyring.dll -resource:unread.png -resource:nounread.png Applet.cs Items.cs TrayLib.cs Preferences.cs

clean:
	rm -f *.exe

