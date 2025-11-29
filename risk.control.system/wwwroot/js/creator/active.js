$(document).ready(function () {
    var jobId = $('#jobId').val(); // Get the job ID from the hidden input field
    var pendingCount = $('#pendingCount').val(); // Get the pending allocations from the hidden input field
    var table = $("#customerTable").DataTable({
        ajax: {
            url: '/api/Investigation/GetActive',
            type: 'GET',
            dataType: 'json',
            data: function (d) {
                console.log("Data before sending:", d); // Debugging

                return {
                    draw: d.draw || 1,
                    start: d.start || 0,
                    length: d.length || 10,
                    caseType: $('#caseTypeFilter').val() || "",  // Send selected filter value
                    search: d.search?.value || "", // Instead of empty string, send "all"
                    orderColumn: d.order?.[0]?.column ?? 15, // Default to column 15
                    orderDir: d.order?.[0]?.dir || "asc" // Default to ascending
                };
            },
            error: function (xhr, status, error) {
                console.error("AJAX Error:", status, error);
                console.error("Response:", xhr.responseText);
                if (xhr.status === 401 || xhr.status === 403) {
                    window.location.href = '/Account/Login'; // Or session timeout handler
                }
            }
        },
        columnDefs: [
        {
            className: 'max-width-column-name', // Apply the CSS class,
            targets: 0                      // Index of the column to style
        },
        {
            className: 'max-width-column-number', // Apply the CSS class,
            targets: 1                      // Index of the column to style
        },
        {
            className: 'max-width-column-name', // Apply the CSS class,
            targets: 2                      // Index of the column to style
            },
            {
                className: 'max-width-column-name', // Apply the CSS class,
                targets: 6                      // Index of the column to style
            },
        {
            className: 'max-width-column-name', // Apply the CSS class,
            targets: 8                      // Index of the column to style
            },
            {
                className: 'max-width-column-number', // Apply the CSS class,
                targets: 10                      // Index of the column to style
            },
            {
                className: 'max-width-column-name', // Apply the CSS class,
                targets: 9                      // Index of the column to style
            },
            {
                'targets': 16, // Index for the "Case Type" column
                'name': 'policy' // Name for the "Case Type" column
            }],
        order: [[15, 'asc']],
        responsive: true,
        fixedHeader: true,
        processing: true,
        serverSide: true,
        paging: true,
        language: {
            loadingRecords: '&nbsp;',
            processing: '<i class="fas fa-sync fa-spin fa-4x fa-fw"></i><span class="sr-only">Loading...</span>'
        },
        columns: [
            /* Name of the keys from
            data file source */

            {
                "data": "policyNum",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.policyId + '" data-bs-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                "data": "amount",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.amount + '" data-bs-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                "data": "agent",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    return '<span title="' + row.agent + '" data-bs-toggle="tooltip">' + data + '</span>';
                }
                ///<button type="button" class="btn btn-lg btn-danger" data-bs-toggle="popover" title="Popover title" data-content="And here's some amazing content. It's very engaging. Right?">Click to toggle popover</button>
            },
            {
                "data": "pincode",
                "mRender": function (data, type, row) {
                    if (row.pincodeName != '...') {
                        return `
            <div class="map-thumbnail profile-image doc-profile-image">
                <img src="${row.personMapAddressUrl}" 
                     class="thumbnail profile-image doc-profile-image preview-map-image" 
                     data-bs-toggle="modal" 
                     data-target="#mapModal" 
                     data-img='${row.personMapAddressUrl}' 
                     data-title='${row.pincodeName}' />
            </div>`;
                    } else {
                        return '<img src="/img/no-map.jpeg" class="profile-image doc-profile-image" title="No address" data-bs-toggle="tooltip" />';
                    }
                }

            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<div class="map-thumbnail profile-image doc-profile-image">';
                    img += '<img src="' + row.document + '" class="full-map" title="' + row.policyId + '" data-bs-toggle="tooltip"/>'; // Full map image with class 'full-map'
                    img += '<img src="' + row.document + '" class="profile-image doc-profile-image" />'; // Thumbnail image with class 'thumbnail'
                    img += '</div>';
                    return img;
                }
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<div class="map-thumbnail table-profile-image">';
                    img += '<img src="' + row.customer + '" class="full-map" title="' + row.customerFullName + '" data-bs-toggle="tooltip"/>'; // Full map image with class 'full-map'
                    img += '<img src="' + row.customer + '" class="table-profile-image" />'; // Thumbnail image with class 'thumbnail'
                    img += '</div>';
                    return img;
                }
            },
            {
                "data": "name",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.name + '" data-bs-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {

                    var img = '<div class="map-thumbnail table-profile-image">';
                    img += '<img src="' + row.beneficiaryPhoto + '" class=" table-profile-image" />'; // Thumbnail image with class 'thumbnail'
                    img += '<img src="' + row.beneficiaryPhoto + '" class="full-map" title="' + row.beneficiaryFullName + '" data-bs-toggle="tooltip"/>'; // Full map image with class 'full-map'
                    img += '</div>';
                    return img;
                }
            },
            {
                "data": "beneficiaryName",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.beneficiaryName + '" data-bs-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "serviceType",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.serviceType + '" data-bs-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "subStatus",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.subStatus + '" data-bs-toggle="tooltip"><small>' + data + '</small></span>'
                }
            },
            {
                "data": "created",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.created + '" data-bs-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var buttons = "";
                    buttons += '<span class="checkbox">';
                    if (row.autoAllocated) {
                        buttons += '<i class="fas fa-cog fa-spin" title="AUTO ALLOCATION" data-bs-toggle="tooltip"></i>';
                    } else {
                        buttons += '<i class="fas fa-user-tag" title="MANUAL ALLOCATION" data-bs-toggle="tooltip"></i>';
                    }
                    buttons += '</span>';
                    
                    return buttons;
                }
            },
            {
                "data": "timePending",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.timePending + '" data-bs-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var buttons = "";
                    buttons += '<a id="details' + row.id + '" href="ActiveDetail?Id=' + row.id + '" class="active-claims btn btn-xs btn-info"><i class="fa fa-search"></i> Detail</a>&nbsp;'

                    if (row.autoAllocated) {

                    }
                    //if (row.withdrawable) {
                    //    buttons += '<a href="withdraw?Id=' + row.id + '" class="btn btn-xs btn-danger"><i class="fa fa-trash"></i> Withdraw</a>&nbsp;'
                    //}
                    return buttons;
                }
            },
            { "data": "timeElapsed", bVisible: false },
            { "data": "policy", bVisible: false }
        ],
        rowCallback: function (row, data, index) {
            if (data.isNew) {
                $('td', row).addClass('isNewAssigned');
                // Remove the class after 3 seconds
                setTimeout(function () {
                    $('td', row).removeClass('isNewAssigned');
                }, 3000);
            }
        },
        "drawCallback": function (settings, start, end, max, total, pre) {

            $('#customerTable tbody').on('click', '.btn-info', function (e) {
                e.preventDefault(); // Prevent the default anchor behavior
                var id = $(this).attr('id').replace('details', ''); // Extract the ID from the button's ID attribute
                getdetails(id); // Call the getdetails function with the ID
                window.location.href = $(this).attr('href'); // Navigate to the delete page
            });
            // Reinitialize Bootstrap 5 tooltips
            var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
            tooltipTriggerList.map(function (el) {
                return new bootstrap.Tooltip(el, {
                    html: true,
                    sanitize: false   // ⬅⬅⬅ THIS IS THE FIX
                });
            });
        }
    });
    // Case Type Filter
    $('#caseTypeFilter').on('change', function () {
        table.ajax.reload(); // Reload the table when the filter is changed
    });
    $('#refreshTable').click(function () {
        var $icon = $('#refreshIcon');
        if ($icon) {
            $icon.addClass('fa-spin');
        }
        table.ajax.reload(null, false); // false => Retains current page
    });

    table.on('xhr.dt', function () {
        $('#refreshIcon').removeClass('fa-spin');
    });
 
    $(document).on('show.bs.modal', '#mapModal', function (event) {
        var trigger = $(event.relatedTarget); // The <img> clicked
        var imageUrl = trigger.data('img');
        var title = trigger.data('title');

        var modal = $(this);
        modal.find('#modalMapImage').attr('src', imageUrl);
        modal.find('.modal-title').text(title || 'Map Preview');
    });

    $('#customerTable tbody').hide();
    $('#customerTable tbody').fadeIn(2000);


    function TrackProgress() {
        let progressBar = document.getElementById("progressBar");
                let progressContainer = document.getElementById("progressContainer");

                // Remove 'hidden' class to show progress bar
                progressContainer.classList.remove("hidden");

                let interval = setInterval(() => {
                    fetch(`/InvestigationPost/GetAssignmentProgress?jobId=${uploadId}`)
                        .then(response => response.json())
                        .then(data => {
                            let progress = data.progress;
                            progressBar.style.width = progress + "%";
                            progressBar.innerText = progress + "%";

                            if (progress >= 100) {
                                clearInterval(interval);
                                setTimeout(() => {
                                    progressContainer.classList.add("hidden"); // Hide after 1 sec
                                }, 1000);
                            }
                        });
                }, 1000);
    }
    if (jobId) {
        checkJobStatus(jobId);
    }
});
let finalCheckAttempts = 0; // Counter for final checks
function getdetails(id) {
    $("body").addClass("submit-progress-bg");
    // Wrap in setTimeout so the UI
    // can update the spinners
    setTimeout(function () {
        $(".submit-progress").removeClass("hidden");
    }, 1);
    
    $('a#details' + id + '.btn.btn-xs.btn-info').html("<i class='fas fa-sync fa-spin'></i> Detail");
    disableAllInteractiveElements()
    var article = document.getElementById("article");
    if (article) {
        var nodes = article.getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    }
}
function showedit(id) {
    
    $('a#edit' + id + '.btn.btn-xs.btn-warning').html("<i class='fas fa-sync fa-spin'></i> Edit");
    disableAllInteractiveElements();

    var article = document.getElementById("article");
    if (article) {
        var nodes = article.getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    }
}

function checkJobStatus(jobId) {
    if (!jobId) {
        console.error("Invalid jobId provided.");
        return;
    }
    $.ajax({
        url: '/CaseActive/GetJobStatus?jobId=' + jobId,
        type: 'GET',
        success: function (response) {
            console.log("Job Status:", response.status);

            if (response.status === "Processing" || response.status === "Enqueued") {
                setTimeout(function () {
                    checkJobStatus(jobId); // Continue polling every 2 sec
                }, 2000);
            } else if (response.status === "Completed" || response.status === "Succeeded") {
                console.log("Job Completed:", response.status);
                    $('#refreshTable').click(); // Refresh the table after completion
            } else {
                console.warn("Job has an issue:", response.status);
                $.confirm({
                    title: 'Job Error',
                    content: 'The job is in an unexpected state: ' + response.status + '. Refresh the table?',
                    buttons: {
                        OK: function () {
                            // Refresh the table when OK is clicked
                            $('#refreshTable').click();
                        }
                    }
                });
            }
        },
        error: function (xhr, status, error) {
            console.error("Error checking job status:", error);
        }
    });
}

window.addEventListener('beforeunload', function () {
    // Check if there is a query string
    if (window.location.search) {
        // Create the URL without the query string
        var newUrl = window.location.protocol + "//" + window.location.host + window.location.pathname;

        // Replace the URL in the browser without the query string
        window.history.replaceState({}, document.title, newUrl);
    }
});

