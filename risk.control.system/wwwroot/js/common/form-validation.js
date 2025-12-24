
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

    

});

