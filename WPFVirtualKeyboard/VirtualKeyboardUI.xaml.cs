using WPFVirtualKeyboard.Control;

namespace WPFVirtualKeyboard
{
    /// <summary>
    /// VirtualKeyboardUI.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class VirtualKeyboardUI : VirtualKeyboard
    {
        public VirtualKeyboardUI()
        {
            InitializeComponent();

            IsEnableHook = false;
        }
    }
}
