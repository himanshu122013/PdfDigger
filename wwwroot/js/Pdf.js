// wwwroot/js/Pdf.js
$(document).ready(function () {
    $('#downloadLink').on('click', function () {
        debugger;
        console.log("Download link clicked");
        // Set the success message text
        $('#successMessage').text("PDF downloaded successfully.");
        // Fade out the success message after 2 seconds (2000 ms)
        setTimeout(function () {
            $('#successMessage').fadeOut();
        }, 2000);
        // Fade out the download link immediately
        $('#downloadLink').fadeOut();
    });
});
