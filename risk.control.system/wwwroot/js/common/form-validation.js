
$(document).ready(function () {
    // Call validateInput with selectors and patterns
    validateInput('#ContractNumber', /[^a-zA-Z0-9]/g); // Allow only alphanumeric characters (no spaces)
    validateInput('#PolicyDetail_CauseOfLoss, #Description, #Addressline, #Comments', /[^a-zA-Z0-9 ]/g); // Allow alphanumeric and spaces
    validateInput('#CustomerName, #BeneficiaryName, #CountryId, #StateId, #DistrictId, #FirstName, #LastName', /[^a-zA-Z ]/g); // Allow alphabets and spaces

    // Allow only numbers and a single dot
    // Allow only numbers and a single dot for SumAssuredValue and ContactNumber
    validateInput('#SumAssuredValue, #ContactNumber', /[^0-9.]/g, function(value) {
        return value.replace(/(\..*)\./g, '$1'); // Prevent multiple dots
    });

   
    // Example usage: Validate file input for allowed types (.jpg, .png, .pdf)
    validateFileInput('#createImageInput', ['jpg', 'png', 'jpeg']); // Adjust the selector and extensions as per your needs

    $('select[required], input[required], input[type="file"], #PinCodeId').on('change input', function () {
        checkFormCompletion('#create-form');
        checkFormCompletion('#edit-form');
    });
    $('select[required], input[required], input[type="file"], #PinCodeId').on('blur', function () {
        checkFormCompletion('#create-form');
        checkFormCompletion('#edit-form');
    });

    // Initially check the form when the page loads
    checkFormCompletion('#create-form');
    checkFormCompletion('#edit-form');

});
