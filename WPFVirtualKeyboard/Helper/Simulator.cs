using WindowsInput;

namespace WPFVirtualKeyboard.Helper
{
    internal class Simulator
    {
        static Simulator()
        {
            Input = new InputSimulator();
            Keyboard = new KeyboardSimulator(Input);
        }

        internal static InputSimulator Input { get; private set; }
        internal static KeyboardSimulator Keyboard { get; private set; }
    }
}
