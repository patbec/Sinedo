
class Debug {

    /**
     * Beginnt die Anwendung zu laden.
     */
    public static Show(): Boolean {
        var snackbar = Application.Services.NotificationControl.current;

        snackbar.addException("Test");
        snackbar.addException("Test");
        snackbar.addException("Test");

        return true;
    }
}