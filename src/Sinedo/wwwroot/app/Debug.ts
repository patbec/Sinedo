
class Debug {

    /**
     * Dieser Code kann mit 'Debug.Show()' über die Browser-Console aufgerufen werden.
     */
    public static Show(): Boolean {
        var snackbar = Application.Services.NotificationControl.current;

        snackbar.addException("Test");
        snackbar.addException("Test");
        snackbar.addException("Test");

        return true;
    }
}