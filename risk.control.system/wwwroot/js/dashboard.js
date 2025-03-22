$(document).ready(function () {
    GetWeekly('Case', 'GetWeeklyClaim', 'container-claim');
    GetWeeklyTat('Case', 'GetClaimWeeklyTat', 'container-claim-tat');
    GetWeeklyPie('Case', 'GetWeeklyClaim', 'container-claim-pie');
    GetWeeklyPie('Agency ', 'GetAgentClaim', 'container-agency-pie');

    GetChart('Case', 'GetClaimChart', 'container-monthly-claim')

    $("#btnWeeklyReport").click(function () {
        GetWeekly('Case', 'GetWeeklyClaim', 'container-claim');
    })

    $("#btnMonthlyReport").click(function () {
        GetMonthly('Case', 'GetMonthlyClaim', 'container-claim');
    })
    $("#btnWeeklyPie").click(function () {
        GetWeeklyPie('Case', 'GetWeeklyClaim', 'container-claim-pie');
    })
    $("#btnMonthlyPie").click(function () {
        GetMonthlyPie('Case', 'GetMonthlyClaim', 'container-claim-pie');
    })
    $("#btnWeeklyTat").click(function () {
        GetWeeklyTat('Case', 'GetClaimWeeklyTat', 'container-claim-tat');
    })
    $("#btnMonthlyTat").click(function () {
        //GetMonthly('Case', 'GetClaimWeeklyTat', 'container-claim-tat');
    })

    $('.details-page').on('click', function () {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);

        $('.details-page').attr('disabled', 'disabled');
        $('html').css('cursor', 'not-allowed');

        var section = document.getElementById("section");
        if (section) {
            var nodes = section.getElementsByTagName('*');
            for (var i = 0; i < nodes.length; i++) {
                nodes[i].disabled = true;
            }
        }
    });
});


function createCharts(container, txn, sum, titleText, totalspent) {
    Highcharts.chart(container, {
        credits: {
            enabled: false
        },
        chart: {
            type: 'pie'
        },
        title: {
            text: titleText + ' ' + totalspent,
            style: {
                fontSize: '.9rem',
                fontFamily: 'Arial Narrow, sans-serif'
            }
        },
        xAxis: {
            type: 'category',
            labels: {
                rotation: -45,
                style: {
                    fontSize: '12px',
                    fontFamily: 'Arial Narrow, sans-serif'
                }
            }
        },
        yAxis: {
            min: 0,
            title: {
                text: txn + ' Count'
            }
        },
        legend: {
            enabled: false
        },
        tooltip: {
            pointFormat: 'Total ' + txn + ': Count <b>{point.y} </b>'
        },
        series: [{
            type: 'pie',
            data: sum,
        }]
    });
}
function createChartColumn(container, txn, sum, titleText, totalspent) {
    Highcharts.chart(container, {
        credits: {
            enabled: false
        },
        chart: {
            type: 'column'
        },
        title: {
            text: titleText + ' ' + totalspent,
            style: {
                fontSize: '.9rem',
                fontFamily: 'Arial Narrow, sans-serif'
            }
        },
        xAxis: {
            type: 'category',
            labels: {
                rotation: -45,
                style: {
                    fontSize: '12px',
                    fontFamily: 'Arial Narrow, sans-serif'
                }
            }
        },
        yAxis: {
            min: 0,
            title: {
                text: txn + ' Count'
            }
        },
        legend: {
            enabled: false
        },
        tooltip: {
            pointFormat: 'Total ' + txn + ': Count <b>{point.y} </b>'
        },
        series: [{
            type: 'column',
            data: sum,
        }]
    });
}
function createMonthChart(container, titleText, data, keys, total) {
    Highcharts.chart(container, {
        credits: {
            enabled: false
        },
        chart: {
            marginRight: 0
        },
        title: {
            text: 'Total ' + titleText + ' Count ' + total,
            style: {
                fontSize: '1rem',
                fontFamily: 'Arial Narrow, sans-serif'
            }
        },
        legend: {
            enabled: false
        },
        xAxis: {
            categories: keys
        },
        yAxis: {
            min: 0,
            title: {
                text: ' Count'
            }
        },
        series: [{
            data: data,
            color: 'green'
        }]
    });
}

function GetChart(title, url, container) {
    var titleMessage = "Last 12 month " + title + ":Count";
    $.ajax({
        type: "GET",
        url: "/Dashboard/" + url,
        contentType: "application/json",
        dataType: "json",
        success: function (result) {
            if (result) {
                var keys = Object.keys(result);
                var weeklydata = new Array();
                var totalspent = 0.0;
                for (var i = 0; i < keys.length; i++) {
                    var arrL = new Array();
                    arrL.push(keys[i]);
                    arrL.push(result[keys[i]]);
                    totalspent += result[keys[i]];
                    weeklydata.push(arrL);
                }
                createMonthChart(container, title, weeklydata, keys, totalspent);
            }
        }
    })
}

function GetWeekly(title, url, container) {
    var titleMessage = "All Current " + title + ":Grouped by status";
    $.ajax({
        type: "GET",
        url: "/Dashboard/" + url,
        contentType: "application/json",
        dataType: "json",
        success: function (result) {
            if (result) {
                var keys = Object.keys(result);
                var weeklydata = new Array();
                var totalspent = 0.0;
                for (var i = 0; i < keys.length; i++) {
                    var arrL = new Array();
                    arrL.push(keys[i]);
                    arrL.push(result[keys[i]]);
                    totalspent += result[keys[i]];
                    weeklydata.push(arrL);
                }
                createChartColumn(container, title, weeklydata, titleMessage, totalspent);
            }
        }
    })
}
function GetWeeklyPie(title, url, container) {
    var titleMessage = "All Current " + title + ":Count";
    $.ajax({
        type: "GET",
        url: "/Dashboard/" + url,
        contentType: "application/json",
        dataType: "json",
        success: function (result) {
            if (result) {
                var keys = Object.keys(result);
                var weeklydata = new Array();
                var totalspent = 0.0;
                for (var i = 0; i < keys.length; i++) {
                    var arrL = new Array();
                    arrL.push(keys[i]);
                    arrL.push(result[keys[i]]);
                    totalspent += result[keys[i]];
                    weeklydata.push(arrL);
                }

                createCharts(container, title, weeklydata, titleMessage, totalspent);
            }
        }
    })
}

function GetMonthly(title, url, container) {
    var titleMessage = "All Current " + title + "Count by status";

    $.ajax({
        type: "GET",
        url: "/Dashboard/" + url,
        contentType: "application/json",
        dataType: "json",
        success: function (result) {
            if (result) {
                var keys = Object.keys(result);
                var monthlydata = new Array();
                var totalspent = 0.0;
                for (var i = 0; i < keys.length; i++) {
                    var arrL = new Array();
                    arrL.push(keys[i]);
                    arrL.push(result[keys[i]]);
                    totalspent += result[keys[i]];
                    monthlydata.push(arrL);
                }
                createChartColumn(container, title, monthlydata, titleMessage, totalspent);
            }
        }
    })
}
function GetMonthlyPie(title, url, container) {
    var titleMessage = "All Current " + title + "Count by status";

    $.ajax({
        type: "GET",
        url: "/Dashboard/" + url,
        contentType: "application/json",
        dataType: "json",
        success: function (result) {
            if (result) {
                var keys = Object.keys(result);
                var monthlydata = new Array();
                var totalspent = 0.0;
                for (var i = 0; i < keys.length; i++) {
                    var arrL = new Array();
                    arrL.push(keys[i]);
                    arrL.push(result[keys[i]]);
                    totalspent += result[keys[i]];
                    monthlydata.push(arrL);
                }

                createCharts(container, title, monthlydata, titleMessage, totalspent);
            }
        }
    })
}

function createChartTat(container, txn, sum, titleText, totalspent) {
    Highcharts.chart(container, {
        credits: {
            enabled: false
        },
        chart: {
            type: 'column'
        },
        title: {
            text: titleText + ' ' + totalspent,
            style: {
                fontSize: '.9rem',
                fontFamily: 'Arial Narrow, sans-serif'
            }
        },
        xAxis: {
            categories: ['0 Day', '1 Day', '2 Day', '3 Day', '4 Day', '5 plus Day']
        },
        yAxis: {
            min: 0,
            title: {
                text: txn + ' Status changes '
            }
        },
        legend: {
            enabled: true
        },
        tooltip: {
            pointFormat: 'Total ' + txn + ': Status changes <b>{point.y} </b>'
        },
        series: sum
    });
}
function GetWeeklyTat(title, url, container) {
    var titleMessage = "All Current " + title + ":Status changes";
    $.ajax({
        type: "GET",
        url: "/Dashboard/" + url,
        contentType: "application/json",
        dataType: "json",
        success: function (result) {
            if (result) {
                var keys = Object.keys(result);
                var weeklydata = new Array();
                var totalspent = 0.0;
                for (var i = 0; i < keys.length; i++) {
                    var arrL = new Array();
                    arrL.push(keys[i]);
                    arrL.push(result[keys[i]]);
                    totalspent += result[keys[i]];
                    weeklydata.push(arrL);
                }

                createChartTat(container, title, result.tatDetails, titleMessage, result.count);
            }
        }
    })
}