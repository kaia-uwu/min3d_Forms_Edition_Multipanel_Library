using System.Runtime.InteropServices;

namespace min3d_Forms_Edition_Multipanel_Library
{
    public static class winapi
    {
        #region GetKeyState
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern short GetKeyState(int nVirtKey);
        #endregion

        #region GetKeyboardState
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern bool GetKeyboardState([Out] byte[] lpKeyState);
        #endregion
    }
}
