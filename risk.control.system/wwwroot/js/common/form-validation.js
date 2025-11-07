
$(document).ready(function () {

    $('#PhoneNumber, #PinCodeId').on("cut copy paste", function (e) {
        e.preventDefault();
    });

    // Call validateInput with selectors and patterns
    validateInput('#emailAddress', /[^a-z]/g); // Allow only alphabet characters (no spaces)
    validateInput('#ContractNumber, #IFSCCode, #Code', /[^a-zA-Z0-9]/g); // Allow only alphanumeric characters (no spaces)
    validateInput('#CauseOfLoss, #Description, #Addressline, #Comments', /[^a-zA-Z0-9 .,-]/g); // Allow alphanumeric, spaces, comma, dot, dash
    validateInput('#CustomerName, #BeneficiaryName, #StateId, #DistrictId, #FirstName, #LastName, #Name, #Branch, #BankName', /[^a-zA-Z ]/g); // Allow alphabets and spaces
    validateInput('#PhoneNumber, #BankAccountNumber, #ISDCode,#mobile', /[^0-9]/g); // Allow numeric only no spaces

    // Allow only numbers and a single dot
    validateInput('#SumAssuredValue, #Price', /[^0-9.]/g, function(value) {
        return value.replace(/(\..*)\./g, '$1'); // Prevent multiple dots
    });
   
    // Example usage: Validate file input for allowed types (.jpg, .png, .pdf)
    validateFileInput('#createImageInput', ['jpg', 'png', 'jpeg']); // Adjust the selector and extensions as per your needs

    // Common selector for required fields and specific inputs
    const requiredFieldsSelector = '#emailAddress, #mailAddress, #domainAddress, select[required], input[required], input[type="file"], input[type="checkbox"], #Comments, #PinCodeId';

    // Add event handlers for 'change' and 'blur' events
    $(requiredFieldsSelector).on('input change blur', function () {
        // Check form completion for the specified forms
        checkFormCompletion('#create-form', true);
        checkFormCompletion('#edit-form');
    });

    // Initially check the form when the page loads
    checkFormCompletion('#create-form', true);
    checkFormCompletion('#edit-form');

    const $emailInput = $("#emailAddress");

    // Handle blur event
    if ($emailInput) {
        $emailInput.on("blur", function () {

            if ($(this).val()) {
                CheckIfEmailValid($(this).val());
            }
        });

        // Handle keydown event
        $emailInput.on("keydown", function (event) {
            return alphaOnly(event);
        });

        // Handle click event
        $emailInput.on("click", function () {
            $(this).select();
        });
    }
    $('#check-domain').on('click', function () {
        checkDomain();
    });

});
function CheckIfEmailValid() {
    var name = $('#emailAddress').val();
    if (name && name.length > 4) {
        $('#check-email').prop('disabled', false);
    }
    else {
        $('#check-email').css('disabled', true);
    }
}

// Example function: Allow only alphabetical characters
function alphaOnly(event) {
    const key = event.key;
    if (!/^[a-zA-Z]$/.test(key) && key !== "Backspace" && key !== "Tab") {
        event.preventDefault();
        return false;
    }
    return true;
}
function checkDomain() {
    $("#domainAddress").val('');
    $('#mailAddress').val('');

    var url = "/Account/CheckAgencyName";
    var name = $('#emailAddress').val().toLowerCase();
    var domain = $('#domain').val().toLowerCase();
    if (name) {
        $('#result').fadeOut(1000); // 1.5 seconds
        $('#result').fadeOut('slow'); // 1.5 seconds
        $.get(url, { input: name, domain: domain }, function (data) {
            if (data == 0) { //available
                $('#mailAddress').val($('#emailAddress').val());
                $('#domainName').val($('#domain').val());
                var mailDomain = $('#domain').val();
                $("#domainAddress").val(mailDomain);
                $("#result").html("<span class='available' title='Available' data-toggle='tooltip'> <i class='fas fa-check'></i></span>");
                $('#result').addClass('result-padding');
                $("#emailAddress").removeClass('error-border');
                $('#result').fadeIn(1000); // 1.5 seconds
                $('#result').fadeIn('slow'); // 1.5 seconds
                $("#Name").focus();
            }
            else if (data == 1) {//domain exists
                $("#domainAddress").val('');
                $('#mailAddress').val('');
                $("#result").html("<span class='unavailable' title='Email exists' data-toggle='tooltip'><i class='fa fa-times-circle'></i></span>");
                $('#result').addClass('result-padding');
                $("#emailAddress").addClass('error-border');
                $('#result').fadeIn(1000); // 1.5 seconds
                $('#result').fadeIn('slow'); // 1.5 seconds
            }
            else if (data = null || data == undefined) {
                $("#domainAddress").val('');
                $('#mailAddress').val('');
            }
        });
    }
}
