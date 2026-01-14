$(document).ready(function () {
    // 1. Validation & Spinner
    $("#login-form").validate({
        submitHandler: function (form) {
            // 1. Trigger visuals immediately
            $("body").addClass("submit-progress-bg");
            $(".submit-progress").removeClass("hidden");

            $('#otp').html('<span class="fas fa-sync fa-spin" aria-hidden="true"></span> Sending...');
            $('#otp').attr('disabled', 'disabled').addClass('login-disabled');
            $('html a, .text').addClass('anchor-disabled');

            // 2. Wrap submit in a 10ms timeout to give the browser a chance to render the spinner
            setTimeout(function () {
                form.submit();
            }, 10);
        }
    });

    // 2. Format Mobile Input
    $(document).on('input', '#MobileNumber', function () {
        this.value = this.value.replace(/[^0-9]/g, '');
    });

    // 3. Country Autocomplete
    $("#CountryIsd").autocomplete({
        source: function (request, response) {
            $.ajax({
                url: "/api/MasterData/GetIsdCode",
                type: "GET",
                data: { term: request.term },
                success: function (data) {
                    response($.map(data, function (item) {
                        return {
                            label: item.label,
                            value: item.isdCode,
                            flag: item.flag
                        };
                    }));
                }
            });
        },
        minLength: 1,
        select: function (event, ui) {
            $(this).val(ui.item.value);
            $("#country-flag").attr("src", ui.item.flag);
            return false;
        }
    });

    // Initial Focus
    $("#CountryIsd").focus();
});
document.addEventListener("DOMContentLoaded", function () {

    // Reference to the modal and close button
    var termsModal = document.getElementById('termsModal');
    var closeTermsButton = document.getElementById('closeterms');
    // Select all elements with the class 'termsLink'
    var termsLinks = document.querySelectorAll('.termsLink');

    // Add a click event listener to each element
    termsLinks.forEach(function (termsLink) {
        termsLink.addEventListener('click', function (e) {
            e.preventDefault(); // Prevent default link behavior (i.e., not navigating anywhere)

            // Show the terms modal
            var termsModal = document.querySelector('#termsModal');
            termsModal.classList.remove('hidden-section');
            termsModal.classList.add('show');
        });
    });
    if (closeTermsButton) {
        // Close the modal when clicking the close button
        closeTermsButton.addEventListener('click', function () {
            termsModal.classList.add('hidden-section'); // Remove the 'show' class to hide the modal
            termsModal.classList.remove('show'); // Remove the 'show' class to hide the modal
        });
    }

    // Optionally, you can close the modal if clicked outside the modal content
    window.addEventListener('click', function (e) {
        if (e.target === termsModal) {
            termsModal.classList.add('hidden-section'); // Remove the 'show' class to hide the modal
            termsModal.classList.remove('show'); // Close the modal if clicked outside
        }
    });

});

