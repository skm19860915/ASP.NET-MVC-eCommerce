var searchred = '';
var overridePush = false;
var searchMode = true;
var numShown = 0;
var num = 0;
var elm = $("<h2>").text("DeactivatedProducts").append( $("<hr>").addClass("dividerHR"));
var outPut = [];
var loader = $("#deactEnd");
var container = $("#deactContainer");

function enableUnveil() {
    console.log("unveiling...")
    $("img.seeAllPic").unveil(0, function () {
        $(this).load(function () {
            $(this).parent().parent().removeClass("isLoading")//.addClass("isLoaded");
        });
    });
    $("img.seeAllPic").on('error', function () { this.src = '/Library/Uploads/WebThumb/No_Image.jpg' });
}

function getDisabled(num) {
    if (searchred.length) {
        $.get("/Home/SearchDeactivated", { searchcred: searchred, page: num }).
        done(function (data) {
            if (data.indexOf("No Product Found") < 0) {
                if (num == 0)
                    container.append(elm);
                outPut.push(data);
                if (overridePush)
                    showDisabled();
                getDisabled(++num);
            }
            else {
                searchred = ''
            }
        });
    }
}

function showDisabled() {
    if (searchMode && (!loader.is(":visible") || overridePush)) {
        if (($(window).scrollTop() + $(window).height() == $(document).height()) || overridePush) {
            overridePush = false;
            loader.show();
            if (numShown < outPut.length) {
                container.append(outPut[numShown]);
                enableUnveil();
                ++numShown;
                loader.hide();
            }
            else
                overridePush = true;
        }
    }
    if (searchred == '') {
        loader.hide();
    }
}


$(function () {
    if ($("#searchcred").length > 0) {
        searchred = $("#searchcred").html();
        searchMode = searchred != '';
        getDisabled(num);
        $(window).scroll(function () {
            showDisabled();
        });
    }
});