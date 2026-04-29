using System.ComponentModel.DataAnnotations;
using Bunit;
using SuperUI.Components;

namespace SuperUI.Tests;

public sealed class SgPropertyGridTests : BunitContext
{
    [Fact]
    public void ShowsValidationMessageForInvalidRequiredField()
    {
        var model = new PropertyGridModel();

        var cut = Render<SgPropertyGrid>(parameters => parameters
            .Add(x => x.SelectedObject, model));

        var editor = cut.FindAll("input.sgc-pg-input").Last();
        editor.Change(string.Empty);

        Assert.Contains("Name is required.", cut.Markup);
        Assert.Contains("sgc-invalid", cut.Markup);
    }

    private sealed class PropertyGridModel
    {
        [Required(ErrorMessage = "Name is required.")]
        public string Name { get; set; } = "Valid";
    }
}
