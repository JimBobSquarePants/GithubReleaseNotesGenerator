// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
$(function () {

    // Wire up the clipboard.
    var clipboard = new ClipboardJS(".btn-clipboard", {
        target: function (trigger) {
            return trigger.parentNode.nextElementSibling;
        }
    });

    clipboard.on("success", function (e) {
        e.trigger.classList.add("btn-success");
    });

    // Scroll results into screen.
    var notes = "releasenotes";
    var anchor = document.getElementById(notes);
    if (anchor) {
        window.location.hash = "#" + notes;
        anchor.scrollIntoView(true);
    }
});
