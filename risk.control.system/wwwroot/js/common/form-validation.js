
$(document).ready(function () {

    $('input[type="text"]').on("cut copy paste", function (e) {
        e.preventDefault();
    });

    // Call validateInput with selectors and patterns
    validateInput('#emailAddress', /[^a-z]/g); // Allow only alphanumeric characters (no spaces)
    validateInput('#ContractNumber, #IFSCCode', /[^a-zA-Z0-9]/g); // Allow only alphanumeric characters (no spaces)
    validateInput('#Code', /[^a-zA-Z]/g); // Allow only alphabets characters (no spaces)
    validateInput('#PolicyDetail_CauseOfLoss, #Description, #Addressline, #Comments', /[^a-zA-Z0-9 ]/g); // Allow alphanumeric and spaces
    validateInput('#CustomerName, #BeneficiaryName, #StateId, #DistrictId, #FirstName, #LastName, #Name, #Branch, #BankName', /[^a-zA-Z ]/g); // Allow alphabets and spaces
    validateInput('#ContactNumber, #PhoneNumber, #BankAccountNumber', /[^0-9]/g); // Allow numeric only no spaces

    // Allow only numbers and a single dot
    // Allow only numbers and a single dot for SumAssuredValue and ContactNumber
    validateInput('#SumAssuredValue', /[^0-9.]/g, function(value) {
        return value.replace(/(\..*)\./g, '$1'); // Prevent multiple dots
    });

   
    // Example usage: Validate file input for allowed types (.jpg, .png, .pdf)
    validateFileInput('#createImageInput', ['jpg', 'png', 'jpeg']); // Adjust the selector and extensions as per your needs

    // Common selector for required fields and specific inputs
    const requiredFieldsSelector = 'select[required], input[required], input[type="file"], #PinCodeId';

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
    $emailInput.on("blur", function () {

        var email = $(this).val();
        if (email != undefined) {
            CheckIfEmailValid(email);
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


});
// Example function: Check if email is valid
function CheckIfEmailValid(email) {
    if (!email.includes("@")) {
        console.warn("Invalid email address.");
        // Add your logic to handle invalid emails (e.g., display error message)
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
                $("#result").html("<span style='color:green;padding-top:.5rem;'> <i class='fas fa-check' style='color:#298807'></i> </span>");
                $('#result').css('padding', '.5rem')
                $("#emailAddress").css('background-color', '');
                $("#emailAddress").css('border-color', '#ccc');
                //$('#create-agency').prop('disabled', false);
                $('#result').fadeIn(1000); // 1.5 seconds
                $('#result').fadeIn('slow'); // 1.5 seconds
                $("#Name").focus();
            }
            else if (data == 1) {//domain exists
                $("#result").html("<span style='color:red;padding-top:.5rem;display:inline !important'><i class='fa fa-times-circle' style='color:red;'></i>  </span>");
                $('#result').css('padding', '.5rem')
                $('#result').css('display', 'inline')
                $("#emailAddress").css('border-color', '#e97878');
                //$('#create-agency').prop('disabled', 'true !important');
                $('#result').fadeIn(1000); // 1.5 seconds
                $('#result').fadeIn('slow'); // 1.5 seconds
            }
            else if (data = null || data == undefined) {
            }
        });
    }
}
