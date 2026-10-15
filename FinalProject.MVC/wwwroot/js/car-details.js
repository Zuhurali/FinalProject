(function () {
  "use strict";

  function initGallery() {
    var gallery = document.querySelector("[data-car-gallery]");
    if (!gallery) {
      return;
    }

    var mainButton = gallery.querySelector("[data-gallery-open]");
    var mainImage = gallery.querySelector("[data-gallery-main]");
    var counter = gallery.querySelector("[data-gallery-count]");
    var thumbs = Array.prototype.slice.call(gallery.querySelectorAll("[data-gallery-thumb]"));
    var sources = thumbs.length
      ? thumbs.map(function (thumb) { return thumb.getAttribute("data-src"); })
      : [mainImage.getAttribute("src")];
    var baseAlt = (mainImage.getAttribute("alt") || "").replace(/, photo \d+ of \d+$/, "");
    var current = 0;

    function select(index) {
      current = (index + sources.length) % sources.length;
      mainImage.src = sources[current];
      mainImage.alt = baseAlt + ", photo " + (current + 1) + " of " + sources.length;
      mainButton.setAttribute("aria-label", "Open photo " + (current + 1) + " of " + sources.length + " full size");
      if (counter) {
        counter.textContent = current + 1 + " / " + sources.length;
      }
      thumbs.forEach(function (thumb, thumbIndex) {
        var active = thumbIndex === current;
        thumb.classList.toggle("is-active", active);
        thumb.setAttribute("aria-pressed", active ? "true" : "false");
      });
    }

    thumbs.forEach(function (thumb, index) {
      thumb.addEventListener("click", function (event) {
        event.preventDefault();
        event.stopPropagation();
        select(index);
      });
    });

    var lightbox = initLightbox(sources, baseAlt, function (index) { select(index); });
    mainButton.addEventListener("click", function (event) {
      event.preventDefault();
      if (lightbox) {
        lightbox.open(current, mainButton);
      }
    });
  }

  function initLightbox(sources, baseAlt, onChange) {
    var overlay = document.getElementById("vehicle-lightbox");
    if (!overlay) {
      return null;
    }

    var image = overlay.querySelector(".vehicle-lightbox__image");
    var caption = overlay.querySelector(".vehicle-lightbox__caption");
    var closeButton = overlay.querySelector(".vehicle-lightbox__close");
    var index = 0;
    var returnFocus = null;
    var touchStartX = null;
    var touchStartY = null;

    function show(nextIndex) {
      index = (nextIndex + sources.length) % sources.length;
      image.src = sources[index];
      image.alt = baseAlt + ", photo " + (index + 1) + " of " + sources.length;
      caption.textContent = index + 1 + " / " + sources.length;
      onChange(index);
    }

    function open(startIndex, trigger) {
      returnFocus = trigger || document.activeElement;
      show(startIndex);
      overlay.hidden = false;
      document.body.classList.add("vehicle-lightbox-open");
      closeButton.focus();
    }

    function close() {
      if (overlay.hidden) {
        return;
      }
      overlay.hidden = true;
      document.body.classList.remove("vehicle-lightbox-open");
      touchStartX = null;
      if (returnFocus && typeof returnFocus.focus === "function") {
        returnFocus.focus();
      }
    }

    overlay.addEventListener("click", function (event) {
      if (event.target.closest("[data-lightbox-close]")) {
        close();
      } else if (event.target.closest("[data-lightbox-prev]")) {
        show(index - 1);
      } else if (event.target.closest("[data-lightbox-next]")) {
        show(index + 1);
      }
    });

    document.addEventListener("keydown", function (event) {
      if (overlay.hidden) {
        return;
      }
      if (event.key === "Escape") {
        event.preventDefault();
        close();
      } else if (event.key === "ArrowLeft" && sources.length > 1) {
        event.preventDefault();
        show(index - 1);
      } else if (event.key === "ArrowRight" && sources.length > 1) {
        event.preventDefault();
        show(index + 1);
      } else if (event.key === "Tab") {
        var focusable = Array.prototype.slice.call(overlay.querySelectorAll("button"));
        var first = focusable[0];
        var last = focusable[focusable.length - 1];
        if (event.shiftKey && document.activeElement === first) {
          event.preventDefault();
          last.focus();
        } else if (!event.shiftKey && document.activeElement === last) {
          event.preventDefault();
          first.focus();
        }
      }
    });

    overlay.addEventListener("touchstart", function (event) {
      if (event.changedTouches && event.changedTouches[0]) {
        touchStartX = event.changedTouches[0].clientX;
        touchStartY = event.changedTouches[0].clientY;
      }
    }, { passive: true });

    overlay.addEventListener("touchend", function (event) {
      if (touchStartX == null || !event.changedTouches || !event.changedTouches[0] || sources.length < 2) {
        return;
      }
      var deltaX = event.changedTouches[0].clientX - touchStartX;
      var deltaY = event.changedTouches[0].clientY - touchStartY;
      touchStartX = null;
      if (Math.abs(deltaX) < 50 || Math.abs(deltaX) < Math.abs(deltaY)) {
        return;
      }
      show(deltaX > 0 ? index - 1 : index + 1);
    }, { passive: true });

    return { open: open, close: close };
  }

  function initFavorite() {
    var form = document.querySelector("[data-favorite-form]");
    if (!form || !window.fetch) {
      return;
    }

    var button = form.querySelector("[data-favorite-button]");
    var icon = form.querySelector("[data-favorite-icon]");
    var label = form.querySelector("[data-favorite-label]");
    var status = document.querySelector("[data-favorite-status]");

    function render(isFavorite) {
      button.classList.toggle("is-active", isFavorite);
      button.setAttribute("aria-pressed", isFavorite ? "true" : "false");
      button.title = isFavorite ? "Remove this car from your favourites" : "Save this car to your favourites";
      icon.classList.toggle("fa-solid", isFavorite);
      icon.classList.toggle("fa-regular", !isFavorite);
      label.textContent = isFavorite ? "Saved" : "Favourite";
      form.action = isFavorite ? form.getAttribute("data-remove-url") : form.getAttribute("data-add-url");
    }

    form.addEventListener("submit", function (event) {
      event.preventDefault();
      if (button.disabled) {
        return;
      }

      var wasFavorite = button.getAttribute("aria-pressed") === "true";
      button.disabled = true;
      button.classList.add("is-busy");
      if (status) {
        status.textContent = "";
      }

      fetch(wasFavorite ? form.getAttribute("data-remove-url") : form.getAttribute("data-add-url"), {
        method: "POST",
        body: new FormData(form),
        headers: { Accept: "application/json" },
        credentials: "same-origin"
      })
        .then(function (response) {
          if (response.status === 401) {
            window.location.href = "/Account/SignIn?returnUrl=" + encodeURIComponent(window.location.pathname);
            return null;
          }
          return response.json().catch(function () { return {}; }).then(function (body) {
            if (!response.ok) {
              throw new Error(body.message || "Your favourites could not be updated. Please try again.");
            }
            return body;
          });
        })
        .then(function (body) {
          if (!body) {
            return;
          }
          render(body.isFavorite === true);
          if (status) {
            status.textContent = body.isFavorite ? "Saved to your favourites." : "Removed from your favourites.";
          }
        })
        .catch(function (error) {
          render(wasFavorite);
          if (status) {
            status.textContent = error.message;
          }
        })
        .finally(function () {
          button.disabled = false;
          button.classList.remove("is-busy");
        });
    });
  }

  document.addEventListener("DOMContentLoaded", function () {
    initGallery();
    initFavorite();
  });
})();
