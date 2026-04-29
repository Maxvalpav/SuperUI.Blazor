using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace SuperUI.Components
{
    public partial class SgThemeEditor : ComponentBase
    {
        [Inject] private IJSRuntime JS { get; set; } = default!;

        private string _primaryColor = "#2563eb";
        private string _successColor = "#10b981";
        private string _dangerColor = "#ef4444";
        private string _borderRadius = "4px";
        private string _fontSize = "14px";

        private async Task UpdateTheme()
        {
            await JS.InvokeVoidAsync("eval", $@"
                document.documentElement.style.setProperty('--sui-primary', '{_primaryColor}');
                document.documentElement.style.setProperty('--sui-success', '{_successColor}');
                document.documentElement.style.setProperty('--sui-danger', '{_dangerColor}');
                document.documentElement.style.setProperty('--sui-radius', '{_borderRadius}');
                document.documentElement.style.setProperty('--sui-font-size', '{_fontSize}');
            ");
        }

        private async Task ResetTheme()
        {
            _primaryColor = "#2563eb";
            _successColor = "#10b981";
            _dangerColor = "#ef4444";
            _borderRadius = "4px";
            _fontSize = "14px";
            await UpdateTheme();
        }
    }
}
