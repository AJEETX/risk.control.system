$(document).ready(function () {

    $('#customerTable').DataTable({
        processing: true,
        serverSide: true,
        ajax: {
            url: '/Pincodes/GetPincodes',
            type: 'GET',
            dataType: 'json',
            data: function (d) {
                d.search = d.search.value; // Pass the search term
                d.orderColumn = d.order[0].column; // Column index (0, 1, 2, etc.)
                d.orderDirection = d.order[0].dir; // Sorting direction ("asc" or "desc")
            }
        },
        order: [[0, 'asc']],
        fixedHeader: true,
        processing: true,
        paging: true,

        language: {
            loadingRecords: '&nbsp;',
            processing: '<i class="fas fa-sync fa-spin fa-4x fa-fw"></i><span class="sr-only">Loading...</span>'
        },
        columns: [
            { data: 'code' },
            { data: 'name' },
            { data: 'district' },
            { data: 'state' },
            { data: 'country' },
            {
                data: 'pinCodeId',
                render: function (data, type, row) {
                    return `
                                <a class="btn btn-xs btn-warning" href="/Pincodes/Edit/${data}">
                                    <i class="fas fa-pen"></i> Edit
                                </a> &nbsp;
                                <a class="btn btn-xs btn-danger" href="/Pincodes/Delete/${data}">
                                    <i class="fas fa-trash"></i> Delete
                                </a>`;
                }
            }
        ]
    });

    var askConfirmation = true;
    $('#create-form').submit(function (e) {
        if ($('#create-form').valid() && askConfirmation) { // Ensure `valid` is called as a method
            e.preventDefault();
            $.confirm({
                title: "Confirm  Add New",
                content: "Are you sure to add?",

                icon: 'fas fa-map-pin',
                type: 'green',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: " Add New",
                        btnClass: 'btn-success',
                        action: function () {
                            askConfirmation = false;
                            $('#create-form').submit();
                        }
                    },
                    cancel: {
                        text: "Cancel",
                        btnClass: 'btn-default'
                    }
                }
            });
        }
    });

    var askEditConfirmation = true;
    $('#edit-form').submit(function (e) {
        if ($('#edit-form').valid() && askEditConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm Edit",
                content: "Are you sure to edit?",
                icon: 'fas fa-map-pin',

                type: 'orange',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: "Edit ",
                        btnClass: 'btn-warning',
                        action: function () {
                            askEditConfirmation = false;
                            $('#edit-form').submit();
                        }
                    },
                    cancel: {
                        text: "Cancel",
                        btnClass: 'btn-default'
                    }
                }
            });
        }
    })

    var countryId = $("#SelectedCountryId").val();
    var stateId = $("#SelectedStateId").val();
    var districtId = $("#SelectedDistrictId").val();  // This holds the preselected district ID

    // Prefill the district if state and country are pre-filled
    if (countryId && stateId && districtId) {
        fetchDistrict(districtId, stateId, countryId);
    }

    // When country or state changes, fetch districts again
    $("#CountryId, #StateId").on("change", function () {
        countryId = $("#SelectedCountryId").val();
        stateId = $("#SelectedStateId").val();
        fetchDistrict(districtId, stateId, countryId);
    });

    // Function to fetch districts based on StateId and CountryId
    function fetchDistrict(districtId, stateId, countryId) {
        $.ajax({
            url: '/GetDistrictName', // Your backend route to fetch districts
            type: 'GET',
            data: { id: districtId, stateId: stateId, countryId: countryId },
            success: function (data) {
                var districtSuggestions = [];
                if (data) {
                    // Populate the district input with suggestions
                    districtSuggestions.push({
                        label: data.DistrictName,
                        value: data.DistrictId
                    });

                    // Apply autocomplete on the DistrictId input field
                    $("#DistrictId").autocomplete({
                        source: districtSuggestions,
                        select: function (event, ui) {
                            $("#SelectedDistrictId").val(ui.item.value);
                        }
                    });

                    // Set the district name into the input field
                    $("#DistrictId").val(data.DistrictName);
                    $("#SelectedDistrictId").val(data.DistrictId);  // Set the selected district ID
                }
            },
            error: function () {
                console.log("Error fetching district");
            }
        });
    }
});

var country = $('#CountryId');
if (country) {
    country.focus();
}