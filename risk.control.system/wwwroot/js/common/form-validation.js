$(document).ready(function () {
    // Generic input validation function
    function validateInput(selector, regex) {
        $(selector).on('input', function () {
            const value = $(this).val();
            // Remove invalid characters directly using the regex
            $(this).val(value.replace(regex, ''));
        });
    }

    // Call validateInput with selectors and patterns
    validateInput('#ContractNumber', /[^a-zA-Z0-9]/g); // Allow only alphanumeric characters (no spaces)
    validateInput('#PolicyDetail_CauseOfLoss, #Description, #Addressline', /[^a-zA-Z0-9 ]/g); // Allow alphanumeric and spaces
    validateInput('#CustomerName, #BeneficiaryName, #CountryId, #StateId, #DistrictId', /[^a-zA-Z ]/g); // Allow alphabets and spaces

    // Allow only numbers and a single dot
    $('#SumAssuredValue, #ContactNumber').on('input', function () {
        this.value = this.value
            .replace(/[^0-9.]/g, '')  // Allow numbers and a single dot
            .replace(/(\..*)\./g, '$1'); // Prevent multiple dots
    });
});
