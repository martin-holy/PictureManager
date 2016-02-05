var picMouseDownX;
var picMouseDownY;
var picMarginTop;
var picMarginLeft;
var picMouseIsDown;
var picBiggerThanScreen;
var mouseIsDown = false;

//to enable ScriptErrorsSuppressed = true
function noError(){return true;}
window.onerror = noError;

function setContentOfElementById(id, data) {
  document.getElementById(id).innerHTML = data;
}

function AppendToContentOfElementById(id, data) {
  var element = document.getElementById(id);
  if (element !== undefined && element !== null)
    element.insertAdjacentHTML("beforeend", data);
}

function setAttrOfElementById(id, attr, data) {
  document.getElementById(id).setAttribute(attr, data);
}

function setAttrOfElementsByClassName(className, attr, data) {
  var items = document.getElementsByClassName(className);
  items.forEach(function (element, index, array) {
    element.setAttribute(attr, data);
  });
}

function scrollToElementById(id) {
  var e = document.getElementById(id);
  if (isUndefinedOrNull(e)) return;
  e.scrollIntoView();
}

function isUndefinedOrNull(o) {
  return (o === undefined || o === null) ? true : false;
}

function thumbnailsDblClick(event) {
  window.external.ShowFullPicture(event.target.parentElement.id);
}

function thumbnailsMouseOver(event) {
  hideKeywords(event.target.parentElement, true);
}

function thumbnailsMouseOut(event) {
  hideKeywords(event.target.parentElement, false);
}

function hideKeywords(elm, value) {
  if (elm.classList.contains("thumbBox")) {
    var k = elm.getElementsByClassName("keywords")[0];
    if (value)
      k.classList.add("hideIt");
    else 
      k.classList.remove("hideIt");
  }
}

function testt() {
  window.external.Test();
}

function testik(event) {
  
}

function onContextMenu(event) {
  event.preventDefault();
  window.external.OnContextMenu();
}

function showFullPicture(index) {
  window.external.ShowFullPicture(index);
}

function mouseWheelEvent(event) {
  window.external.FullPicMouseWheel(event.wheelDelta);
}

function switchToBrowser() {
  window.external.SwitchToBrowser();
}

function picMouseDownEvent(event) {
  picMouseIsDown = true;
  if (picBiggerThanScreen) {
    var pic = event.currentTarget;
    pic.draggable = false;
    picMouseDownX = event.x;
    picMouseDownY = event.y;

    picMarginTop = Math.round(event.y - ((pic.naturalHeight / pic.height) * (picMouseDownY - pic.offsetTop)));
    picMarginLeft = Math.round(event.x - ((pic.naturalWidth / pic.width) * (picMouseDownX - pic.offsetLeft)));

    pic.style.marginTop = picMarginTop + "px";
    pic.style.marginLeft = picMarginLeft + "px";

    pic.width = pic.naturalWidth;
    pic.height = pic.naturalHeight;
  }
}

function picMouseMoveEvent(event) {
  if (picMouseIsDown && picBiggerThanScreen) {
    var pic = document.getElementById("fullPic");
    if (pic !== undefined && pic !== null) {
      pic.style.marginTop = ((event.y - picMouseDownY) + picMarginTop) + "px";
      pic.style.marginLeft = ((event.x - picMouseDownX) + picMarginLeft) + "px";
    }
  }
}

function picMouseUpEvent() {
  picMouseIsDown = false;
  if (picBiggerThanScreen) {
    var pic = document.getElementById("fullPic");
    if (pic !== undefined && pic !== null) {
      pic.style.marginTop = "0px";
      pic.style.marginLeft = "0px";
    }
    fitPicture();
  }
}

function fitPicture() {
  //BUG: kdyz otevru maly obrazek, tak je v se ok, kdyz pak otervru velky a pak znova maly, tak uz se maly nevycentruje
  var debug = document.getElementById("debug");
  var pic = document.getElementById("fullPic");
  if (pic !== undefined && pic !== null) {
    var bh = window.document.body.clientHeight;
    var bw = window.document.body.clientWidth;
    var pnh = pic.naturalHeight;
    var pnw = pic.naturalWidth;
    var ph;
    var pw;

    debug.innerHTML += "pnh:" + pnh + ", bh:" + bh + ", pnw:" + pnw + ", bw:" + bw + "<br />";

    if ((pnh > bh) || (pnw > bw)) { //picture is bigger than window  
      picBiggerThanScreen = true;
      //pic.classList.remove("centerFullPic");
      pic.className = "";
      debug.innerHTML += "picture is bigger than window<br />";
      if (bh / pnh > bw / pnw) { //landscape
        pw = bw;
        ph = pnh * (bw / pnw);
      } else {
        pw = pnw * (bh / pnh);
        ph = bh;
      }
      pic.height = ph;
      pic.width = pw;
    } else {
      picBiggerThanScreen = false;
      //pic.classList.add("centerFullPic");
      pic.className = "centerFullPic";
      pic.height = pnh;
      pic.width = pnw;
    }
  }
}

function dragStart(event) {
  event.dataTransfer.effectAllowed = "copyMove";
  event.dataTransfer.setData("text", "PictureManager");
}