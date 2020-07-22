// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
$(function () {
    var clipboard = new ClipboardJS(".btn-clipboard", {
        target: function (trigger) {
            return trigger.parentNode.nextElementSibling;
        }
    });

    clipboard.on("success", function (e) {
        e.trigger.classList.add("btn-success");
    });
});
