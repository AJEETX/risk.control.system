$(document).ready(function () {
    $('#customerTable').DataTable({
        processing: true,
        serverSide: true,
        ajax: {
            url: '/District/GetDistricts',
            type: 'GET',
            dataType: 'json',
            data: function (d) {
                d.search = d.search.value; // Pass the search term
                d.orderColumn = d.order[0].column; // Column index
                d.orderDirection = d.order[0].dir; // "asc" or "desc"
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
            { data: 'name' },
            { data: 'state' },
            { data: 'country' },
            { data: 'updated' },
            {
                data: 'districtId',
                render: function (data, type, row) {
                    return `
                                        <a class="btn btn-xs btn-warning" href="/District/Edit/${data}">
                                            <i class="fas fa-pen"></i> Edit
                                        </a> &nbsp;
                                        <a class="btn btn-xs btn-danger" href="/District/Delete/${data}">
                                            <i class="fas fa-trash"></i> Delete
                                        </a>`;
                }
            }
        ]
    });

    var askConfirmation = true;
    $('#create-form').submit(function (e) {
        if (askConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm  Add New",

                content: "Are you sure to add?",
                icon: 'fas fa-city',
                type: 'green',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: " Add New",
                        btnClass: 'btn-success',
                        action: function () {
                            askConfirmation = false;
                            $("body").addClass("submit-progress-bg");
                            // Wrap in setTimeout so the UI
                            // can update the spinners
                            setTimeout(function () {
                                $(".submit-progress").removeClass("hidden");
                            }, 1);
                            // Disable all buttons, submit inputs, and anchors
                            $('button, input[type="submit"], a').prop('disabled', true);

                            // Add a class to visually indicate disabled state for anchors
                            $('a').addClass('disabled-anchor').on('click', function (e) {
                                e.preventDefault(); // Prevent default action for anchor clicks
                            });
                            $('#create').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Add District");

                            $('#create-form').submit();
                            var createForm = document.getElementById("create-form");
                            if (createForm) {

                                var nodes = createForm.getElementsByTagName('*');
                                for (var i = 0; i < nodes.length; i++) {
                                    nodes[i].disabled = true;
                                }
                            }
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
        if (askEditConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm Edit",
                content: "Are you sure to edit?",

                icon: 'fas fa-city',
                type: 'orange',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: "Edit ",
                        btnClass: 'btn-warning',
                        action: function () {
                            askEditConfirmation = false;
                            $("body").addClass("submit-progress-bg");
                            // Wrap in setTimeout so the UI
                            // can update the spinners
                            setTimeout(function () {
                                $(".submit-progress").removeClass("hidden");
                            }, 1);
                            // Disable all buttons, submit inputs, and anchors
                            $('button, input[type="submit"], a').prop('disabled', true);

                            // Add a class to visually indicate disabled state for anchors
                            $('a').addClass('disabled-anchor').on('click', function (e) {
                                e.preventDefault(); // Prevent default action for anchor clicks
                            });
                            $('#edit').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Edit District");
                            $('#edit-form').submit();

                            var createForm = document.getElementById("edit-form");
                            if (createForm) {

                                var nodes = createForm.getElementsByTagName('*');
                                for (var i = 0; i < nodes.length; i++) {
                                    nodes[i].disabled = true;
                                }
                            }
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

    var countryId = $("#CountryId").val();

    // Fetch states if the countryId is pre-filled
    if (countryId) {
        fetchStates(countryId);
    }

    // When country changes, fetch the states again
    $("#CountryId").on("change", function () {
        countryId = $(this).val();
        fetchStates(countryId);
    });

    // Function to fetch states based on countryId
    function fetchStates(countryId) {
        $.ajax({
            url: '/GetStateName',  // Your controller route to fetch states
            type: 'GET',
            data: { countryId: countryId },
            success: function (data) {
                var stateSuggestions = [];
                // Prepare suggestions for autocomplete
                if (data && data.length) {
                    stateSuggestions = data.map(function (state) {
                        return {
                            label: state.StateName,
                            value: state.StateId
                        };
                    });
                }

                // Apply autocomplete on the StateId input field
                $("#StateId").autocomplete({
                    source: stateSuggestions,
                    select: function (event, ui) {
                        // When a state is selected, store the StateId in the hidden input field
                        $("#SelectedStateId").val(ui.item.value);
                    }
                });
            },
            error: function () {
                console.log("Error fetching states");
            }
        });
    }

    // Pre-fill the StateId input if a StateId is set in the model
    var selectedStateId = $("#SelectedStateId").val();  // Replace with the actual stateId from the model
    if (selectedStateId) {
        // Fetch the state name from the backend if needed or directly fill the state name
        $.ajax({
            url: '/api/Company/GetStateNameForCountry',  // Your controller route to fetch state name
            type: 'GET',
            data: { countryId: countryId, id: selectedStateId },
            success: function (data) {
                if (data) {
                    $("#StateId").val(data.stateName);
                    $("#SelectedStateId").val(data.stateId);
                }
            },
            error: function () {
                console.log("Error fetching selected state");
            }
        });
    }

    $('a.create').on('click', function () {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);
        // Disable all buttons, submit inputs, and anchors
        $('button, input[type="submit"], a').prop('disabled', true);

        // Add a class to visually indicate disabled state for anchors
        $('a').addClass('disabled-anchor').on('click', function (e) {
            e.preventDefault(); // Prevent default action for anchor clicks
        });

        $('a.create').html("<i class='fas fa-sync fa-spin'></i> Add District");

        var article = document.getElementById("article");
        if (article) {
            var nodes = article.getElementsByTagName('*');
            for (var i = 0; i < nodes.length; i++) {
                nodes[i].disabled = true;
            }
        }
    });
});

var country = $('#CountryId');
if (country) {
    country.focus();
}