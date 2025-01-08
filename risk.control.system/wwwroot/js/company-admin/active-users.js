$(document).ready(function () {
    var table = $("#customerTable").DataTable({
        ajax: {
            url: '/api/User/ActiveUsers',
            dataSrc: ''
        },
        fixedHeader: true,
        processing: true,
        paging: true,
        language: {
            loadingRecords: '&nbsp;',
            processing: '<i class="fas fa-sync fa-spin fa-4x fa-fw"></i><span class="sr-only">Loading...</span>'
        },
        order: [[12, 'desc'], [13, 'desc']], // Sort by `isUpdated` and `lastModified`,
        columns: [
            /* Name of the keys from
            data file source */
            {
                "data": "id", "name": "Id", "bVisible": false
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<img alt="' + row.name + '" title="' + row.name + '" src="' + row.photo + '" class="table-profile-image" data-toggle="tooltip"/>';
                    return img;
                }
            },
            { "data": "email" },
            { "data": "name" },
            { "data": "phone" },
            { "data": "addressline", bSortable : false },
            { "data": "district" },
            { "data": "state" },
            { "data": "country" },
            { "data": "pincode" },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var buttons = "";
                    buttons += '<span class="checkbox">'
                    if (row.active) {
                        buttons += '<i class="fa fa-toggle-on"></i>';
                    } else {
                        buttons += '<i class="fa fa-toggle-off"></i>';
                    }
                    buttons += '</span>'
                    return buttons;
                }
            },
            { "data": "roles" },
            {
                "data": "isUpdated",
                "bVisible": false
            },
            {
                "data": "lastModified",
                bVisible: false
            }
        ],
        error: function (xhr, status, error) { alert('err ' + error) }
    });
    table.on('draw', function () {
        table.rows().every(function () {
            var data = this.data(); // Get row data
            console.log(data); // Debug row data

            if (data.isUpdated) { // Check if the row should be highlighted
                var rowNode = this.node();

                // Highlight the row
                $(rowNode).addClass('highlight-new-user');

                // Scroll the row into view
                rowNode.scrollIntoView({ behavior: 'smooth', block: 'center' });

                // Optionally, remove the highlight after a delay
                setTimeout(function () {
                    $(rowNode).removeClass('highlight-new-user');
                }, 3000);
            }
        });
    });

    $('#customerTable').on('draw.dt', function () {
        $('[data-toggle="tooltip"]').tooltip({
            animated: 'fade',
            placement: 'top',
            html: true
        });
    });
});

function showroles() {
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

    $('a.btn.btn-info').html("<i class='fas fa-sync fa-spin'></i> Roles");

    var article = document.getElementById("article");
    if (article) {
        var nodes = article.getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    }
}
function showedit(id) {
    $("body").addClass("submit-progress-bg");
    // Wrap in setTimeout so the UI
    // can update the spinners
    setTimeout(function () {
        $(".submit-progress").removeClass("hidden");
    }, 1);
    var editbtn = $('a#edit' + id + '.btn.btn-xs.btn-warning')
    // Disable all buttons, submit inputs, and anchors
    $('button, input[type="submit"], a').prop('disabled', true);

    // Add a class to visually indicate disabled state for anchors
    $('a').addClass('disabled-anchor').on('click', function (e) {
        e.preventDefault(); // Prevent default action for anchor clicks
    });
    editbtn.html("<i class='fas fa-sync fa-spin'></i> Edit");

    var article = document.getElementById("article");
    if (article) {
        var nodes = article.getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    }
}
