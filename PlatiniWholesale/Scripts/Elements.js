document.createElement('article');
document.createElement('aside');
document.createElement('figcaption');
document.createElement('figure');
document.createElement('footer');
document.createElement('header');
document.createElement('hgroup');
document.createElement('nav');
document.createElement('section');

function equalHeight(group) {
    var tallest = 0;
    group.each(function () {
        var thisHeight = $(this).height();
        if (thisHeight > tallest) {
            tallest = thisHeight;
        }
    });
    $(group).css('minHeight', tallest);
    //alert(group);
}

var modal = {};
modal.hide = function () {
    $('#overlay2').fadeOut();
    $('.dialog').fadeOut();
    $("body").css({
        "overflow": "auto"
    });
};
$(document).ready(function () {
    $('input[type="text"],input[type="password"],textarea').focus(function () {
        $(this).select();
    });
    $('input[type="text"],input[type="password"],textarea').mouseup(function () {
        return false;
    });

    if ($.trim($("#dvMessage").html()).length > 0) {
        ShowMessage();
    }
    $(".filtertab").click(function () {
        $("#filterblock").slideToggle("slow");
    });
    $('#SideBar').css('left', 50 - $('#SideBar').width());

    SetSideBarPosition();
    $(window).resize(function () {
        SetSideBarPosition(true);
    });
    // Close dialog when clicking on the "ok-dialog"
    $('.ok-dialog').click(function () {
        modal.hide();
    });
    // Require the user to click OK if the dialog is classed as "modal"
    $('#overlay2').click(function () {
        if ($(modal.id).hasClass('modal')) {
            // Do nothing
        } else {
            modal.hide();
        }
    });
    // Prevent dialog closure when clicking the body of the dialog (overrides closing on clicking overlay)
    $('.dialog').click(function () {
        event.stopPropagation();
    });
});

function ShowAlert(text) {
    $("#dvMessage").html(text);
    ShowMessage();
    //$("#dvMessage").html('');
}

function ShowMessage() {

    modal.id = '#dialog-anchor';
    $(document.body).css({
        "overflow": "hidden"
    });
    $('#overlay2').fadeIn();
    $(modal.id).fadeIn();
}

function toggleMenu() {
    if ($("#pnlmodewrapper").css("right") != '-50px')
        $("#pnlmodewrapper").animate({ "right": '-50px' }, 'slow');
    else
        $("#pnlmodewrapper").animate({ "right": '0px' }, 'slow');
}

function checkUser() {
    if ($.trim($("#Username").val()) != '') {
        $.ajax({
            url: $("#hPageId").val() + 'Home/CheckUser',
            type: "POST",
            data: JSON.stringify({ 'Username': $("#Username").val() }),
            dataType: "json",
            traditional: true,
            contentType: "application/json; charset=utf-8",
            success: function (data) {
                if (data.status == "Success") {
                    ShowAlert(data.message);
                } else {
                    ShowAlert(data.message);
                }
            },
            error: function () {
                ShowAlert("An error has occured!!!");
            }
        });
    }
    else {
        ShowAlert("Username field should not be empty");
    }
}

$(document).ready(function () {
    $('ul#css3menu1 li').hover(
        function () {
            $(this).children('ul#css3menu1, ul#css3menu1 ul').stop().slideDown(250);
        },
        function () {
            $(this).children('ul#css3menu1, ul#css3menu1 ul').stop().slideUp(250);
        }
    );
    $('#slide').click(function () {
        SlideSideBar();
    });

});

$(".switchbox").live('click', function () {
    if ($('ul#css3menu1').is(':visible')) {
        $("ul#css3menu1").stop().slideUp();
    } else {
        $("ul#css3menu1").stop().slideDown();
    }

});

function SetSideBarPosition() {
    var hgt = $(window).height();

    if (hgt < 300)
        hgt = 600;
    $('#SideBar').css('top', 10);

    $('#SideBar').height(hgt - 40);
}

function setCustomerData(Id) {

    $.get('/Home/GetCustomerData', { Id: Id }).done(function (data) {
        $("#getCustomers").removeClass("bgClass3");
        if (data != null && data.length != 0) {
            $("#UserId").val(data.UserId);
            $("#CartOwner_Buyer").val(data.Buyer);
            $("#CartOwner_ShippingAddress_To").val(data.ShippingAddress.To);
            $("#CartOwner_BillingAddress_AddressId").val(data.BillingAddress.AddressId);
            $("#CartOwner_ShippingAddress_AddressId").val(data.ShippingAddress.AddressId);
            $("#CartOwner_BillingAddress_Line1").val(data.BillingAddress.Line1);
            $("#CartOwner_ShippingAddress_Line1").val(data.ShippingAddress.Line1);
            $("#CartOwner_BillingAddress_Line2").val(data.BillingAddress.Line2);
            $("#CartOwner_ShippingAddress_Line2").val(data.ShippingAddress.Line2);
            $("#CartOwner_ShippingAddress_ZipCode").val(data.ShippingAddress.ZipCode);
            $("#CartOwner_Phone").val(data.Phone);
            $("#CartOwner_Fax").val(data.Fax);
            $("#CartOwner_Email").val(data.Email);
            $("#CartOwner_TermId").val(data.TermId);
            $("#CartOwner_ShipVia").val(data.ShipVia);
            $("#CartOwner_isRetail").val(data.isRetail);
            $("#Discount").val(data.Discount);
            $("#CartOwner_SalesPersonId").val(data.SalesPersonId);
            if ($("#SalesmenName").length > 0) {
                $("#SalesmenName").val(data.SalesPerson);
            }
            else {
                $("#getSalesMen").val(data.SalesPerson);
            }
            $("#cartForm").submit();
        }
        else {
            //$.get('/Home/RegistrationAtCart').done(function (data) {
            //    $('#registerDialog').empty().append(data);
            //    $('#registerDialog').dialog("open");
            //});
            $("#registerDialog").colorbox({ iframe: true, width: "80%", height: "95%", transition: "elastic", opacity: .6, open: true, overlayClose: false, href: '/Home/RegistrationAtCart' });
        }

    });
}
function closeMeBox(Id) {
    parent.jQuery.fn.colorbox.close();
    if (parseInt(Id) > 0) {
        parent.setCustomerData(Id);
    }
}
function ActivateAuto() {
    if ($("#getCustomers").length > 0) {
        $.get('/Home/Users').done(function (data) {

            $("#getCustomers").autocomplete({
                source: data,
                minlength: 0,
                response: function (event, ui) {
                    var noResult = {
                        value: "0",
                        label: "New Customer"
                    };
                    ui.content.push(noResult);
                },
                focus: function (event, ui) {
                    $("#getCustomers").val(ui.item.label);
                    return false;
                },
                select: function (event, ui) {
                    $("#getCustomers").val(ui.item.label);
                    $("#getCustomers").addClass("bgClass3");
                    setCustomerData(ui.item.value);
                    return false;
                },
            }).data("ui-autocomplete")._renderItem = function (ul, item) {
                return $("<li>")
                    .append("<a>" + item.label + "<br>" + "</a>")
                    .appendTo(ul);
            };
        });
    }

    if ($("#getSalesMen").length > 0) {
        $.get('/Home/Salesmen').done(function (data) {
            $("#getSalesMen").autocomplete({
                source: data,
                minlength: 0,
                focus: function (event, ui) {
                    $("#getSalesMen").val(ui.item.label);
                    return false;
                },
                select: function (event, ui) {
                    $("#getSalesMen").val(ui.item.label);
                    $("#CartOwner_SalesPersonId").val(ui.item.value);
                    return false;
                }
            }).data("ui-autocomplete")._renderItem = function (ul, item) {
                return $("<li>")
                    .append("<a>" + item.label + "<br>" + "</a>")
                    .appendTo(ul);
            };;
        });
    }

    $.get('/Home/Clothes').done(function (data) {

        $("#getStyles").autocomplete({
            source: data,
            minlength: 3,
            response: function (event, ui) {
                if (ui.content.length == 0) {
                    var noResult = {
                        value: "0",
                        label: "No Product Found"
                    };
                    ui.content.push(noResult);
                    $("#getStyles").attr('data-id', 0);
                }
                else if (ui.content.length == 1) {
                    ui.item = ui.content[0];
                    $(this).data('ui-autocomplete')._trigger('select', 'autocompleteselect', ui);
                }
            },
            open: function (event, ui) {
                var $input = $(event.target);
                var $results = $input.autocomplete("widget");
                var scrollTop = $(window).scrollTop();
                var top = $results.position().top;
                var height = $results.outerHeight();
                if (top + height > $(window).innerHeight() + scrollTop) {
                    newTop = top - height - $input.outerHeight();
                    if (newTop > scrollTop) {
                        $results.css("top", newTop + "px");
                    }
                    else {
                        $results.css("height", "326px")
                        height = $results.outerHeight();
                        if (top + height > $(window).innerHeight() + scrollTop) {
                            newTop = top - height - $input.outerHeight();
                            if (newTop > scrollTop) {
                                $results.css("top", newTop + "px");
                            }
                        }
                    }
                }
            },
            focus: function (event, ui) {
                $("#getStyles").val(ui.item.label);
                $("#getStyles").attr('data-id', ui.item.value);
                return false;
            },
            select: function (event, ui) {
                $("#getStyles").val(ui.item.label);
                $("#getStyles").attr('data-id', ui.item.value);
                $("#addStyles").click();
                return false;
            },
        }).data("ui-autocomplete")._renderItem = function (ul, item) {
            return $("<li>")
                .append("<a>" + item.label + "<br>" + "</a>")
                .appendTo(ul);
        };
    });
}


function GetStyleData(id, isNew) {
    $.get('/Home/GetScales', { Id: id }).done(function (data) {
        if (data != null) {
            $("#getStyles").removeClass("bgClass6");
            if (data.aV == true) {
                if (data.hI == true) {
                    if (data.fI == true) {
                        $("#getStyles").addClass("bgClass6");
                        DetailPartial(id);
                    }
                    else {
                        $("#getStyles").addClass("bgClass6");
                        AddByStyleNo(id, isNew);
                    }
                }
                else {
                    if (data.iA == true) {
                        var dialog = $('<p>There is currently no inventory for this item.  Would you like to edit the inventory for it?</p>').dialog({
                            //title: "Warning!",
                            buttons: {
                                "Yes": function () {
                                    window.location.href = '/Home/Detail/' + id;
                                    return true;
                                },
                                "No": function () { dialog.dialog('close'); }
                            }
                        });                        
                    }
                    else {
                        var dialog = $('<p>Sorry, this item is out of stock.</p>').dialog({
                            //title: "Warning!",
                            buttons: {
                                "OK": function () { dialog.dialog('close'); }
                            }
                        });                        
                    }
                }
            }
            else {
                var dialog = $('<p>Sorry no product was found for this style number.</p>').dialog({
                    //title: "Warning!",
                    buttons: {
                        "OK": function () { dialog.dialog('close'); }
                    }
                });                
            }
        }
    });                      
}

function AddByStyleNo(Id, isNew) {
    $.post('/Home/AddByStyleNo', { Id: Id }).done(function (data) {
        $("#getStyles").removeClass("bgClass6");
        if (data == "1") {
            if (isNew == "False") {
                $("#slide").click();
                setTimeout(function () {
                    $("#slide").click();
                }, 1000);
            }
            else {
               // window.location = window.location.href;
                //$.get('/Home/Cart', { from: 1 }).done(function (data) {
                //});  
                $.ajax({
                    type: "GET",
                    url: "/Home/Cart",
                    data: { "from": 1 },
                    dataType: "html",
                    success: function (data) {
                        $("#model-pop").html(data);
                        //$(".clicker2").val(@SiteConfiguration.OrderCount())
                    },
                    error: function () { }
                });
            }
        }
        else if (data == "0") {
            var dialog = $('<p>Please update the quantity of this style from your shopping cart.</p>').dialog({                
                buttons: {                   
                    "Ok": function () { dialog.dialog('close'); }
                }
            });           
        }
        else if (data == "-1") {
            var dialog = $('<p>Sorry no product was found for this style number.</p>').dialog({
                buttons: {
                    "Ok": function () { dialog.dialog('close'); }
                }
            });            
        }
        else if (data == "2") {
            var dialog = $('<p>Please try again.</p>').dialog({
                buttons: {
                    "Ok": function () { dialog.dialog('close'); }
                }
            });            
            window.location.href = window.location;
        }
    });
}

function GetStyleDataProcess(id, isNew, OrderId) {
    $.get('/Home/GetScales', { Id: id }).done(function (data) {
        if (data != null) {
            $("#getStyles").removeClass("bgClass6");
            if (data.aV == true) {
                if (data.hI == true) {
                    if (data.fI == true) {
                        $("#getStyles").addClass("bgClass6");
                        AddByStyleNoProcess(id, isNew, OrderId);
                    }
                    else {
                        $("#getStyles").addClass("bgClass6");
                        AddByStyleNoProcess(id, isNew, OrderId);
                    }
                }
                else {
                    if (data.iA == true) {
                        var dialog = $('<p>There is currently no inventory for this item.  Would you like to edit the inventory for it?</p>').dialog({
                            //title: "Warning!",
                            buttons: {
                                "Yes": function () {
                                    window.location.href = '/Home/Detail/' + id;
                                    return true;
                                },
                                "No": function () { dialog.dialog('close'); }
                            }
                        });
                        //if (confirm("There is currently no inventory for this item.  Would you like to edit the inventory for it?")) {
                        //    window.location.href = '/Home/Detail/' + id;
                        //}
                    }
                    else {
                        var dialog = $('<p>Sorry, this item is out of stock.</p>').dialog({
                            //title: "Warning!",
                            buttons: {
                                "OK": function () { dialog.dialog('close'); }
                            }
                        });
                        //alert('Sorry, this item is out of stock.');
                    }
                }
            }
            else {
                var dialog = $('<p>Sorry no product was found for this style number.</p>').dialog({
                    //title: "Warning!",
                    buttons: {
                        "OK": function () { dialog.dialog('close'); }
                    }
                });
                //alert('Sorry no product was found for this style number.');
            }
        }
    });
}
function AddByStyleNoProcess(Id, isNew, OrderId) {
    $.post('/Common/Order/AddByStyleNo', { Id: Id, OrderId: OrderId }).done(function (data) {
        $("#getStyles").removeClass("bgClass6");
        if (data == "1") {
            if (isNew == "False") {
                $("#slide").click();
                setTimeout(function () {
                    $("#slide").click();
                }, 1000);
            }
            else {
                window.location = window.location.href;
            }
        }
        else if (data == "0") {
            alert("Please update the quantity of this style from your shopping cart.");
        }
        else if (data == "-1") {
            alert("Sorry no product was found for this style number.");
        }
        else if (data == "2") {
            alert("Please try again");
            window.location.href = window.location;
        }
    });
}

function DetailPartial(Id) {
    $.get('/Home/DetailPartial', { Id: Id, from: "1" }).done(function (data) {
        $("#getStyles").removeClass("bgClass6");
        $('#detPart').html(data).dialog("open");
    });
}


var bOpen = false;
function SlideSideBar() {



    if (bOpen == true) {
        if (askSavingConfirmation() == true) {
            if (confirm('You have unsaved changes, do you wanted to save first?') == true) {
                return false;
            }
        }
        $("#Content").empty();
    }
    else {
        $("#SideBar").addClass("bgClass");
        $.get("/Home/Cart").done(function (data) {
            $("#Content").empty().append(data);
            ActivateAuto();
            $("#SideBar").removeClass("bgClass");
            $("#getStyles").focus();
        });

        //if ($(".clsiframe").contents().find(".myClass").val() == "1") {
        //    var dialog = $('<p>The text boxes in your cart with red color are out of Stock</p>').dialog({
        //        title: "Warning!",
        //        buttons: {                    
        //            "Ok": function () {
        //                dialog.dialog('close');
        //            }
        //        }
        //    });
        //    //alert('The text boxes in your cart with red color are out of Stock');
        //}
    }

    if ($("#SideBar").is(':animated') == false) {

        if ($('#SideBar').offset().left == 0) {//if left is ZERo that means still is in progress.
        }
        else {
            $("#SideBar").stop().animate(
                    { "width": !bOpen ? "+=1196px" : "-=1196px" },
                    "slow",
                    function () {

                        bOpen = !bOpen;
                        if (bOpen == true) {
                            //$('#ClickOutsideCart').show();
                            //Checkliop();
                            //$('#SearchBox').hide();
                        } else {
                            //$('#SearchBox').show();
                            //$('#ClickOutsideCart').hide();
                        }
                    }
                );
        }
    }
    //});
}

function askSavingConfirmation() {
    //$('.clsiframe').
    if ($(".clsiframe").contents().find(".clsIsChange").val() == "1") {
        return true;
    }
    return false;
}

function buildModal() {
    var elm = $("<div>");
    elm.addClass("md-modal md-effect-7 md-show");
    elm.append(
        $("<div>").addClass("md-content popupCOntainer").append(
        $("<div>").addClass("topRor").append(
        $("<div>").addClass("md-close md-destroy").css("text-align", "right").append(
        $("<img>").attr("src", "/Styles/images/cross.png")
        ))));
    var msg = $("<div>").addClass("popinner").html($("#showMsg").text());
    elm.find(".popupCOntainer").append(msg);
    elm.attr("id", "showmessage");
    elm.insertBefore(".md-overlay:last");
}