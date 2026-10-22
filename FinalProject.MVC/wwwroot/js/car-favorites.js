(function () {
  function getToken(form) {
    var field = form.querySelector('input[name="__RequestVerificationToken"]');
    return field ? field.value : "";
  }

  function render(form, isFavorite) {
    var button = form.querySelector("[data-favorite-button]");
    var icon = form.querySelector("[data-favorite-icon]");
    if (!button || !icon) {
      return;
    }

    form.setAttribute("data-is-favorite", isFavorite ? "true" : "false");
    form.action = isFavorite
      ? form.getAttribute("data-remove-url")
      : form.getAttribute("data-add-url");
    button.setAttribute("aria-pressed", isFavorite ? "true" : "false");
    button.title = isFavorite ? "Remove from favorites" : "Add to favorites";
    icon.classList.toggle("fa-solid", isFavorite);
    icon.classList.toggle("fa-regular", !isFavorite);
  }

  function showStatus(message, isError) {
    var status = document.getElementById("favorite-card-status");
    if (!status) {
      return;
    }

    status.hidden = !message;
    status.textContent = message || "";
    status.classList.toggle("is-error", !!isError);
  }

  function bindForm(form) {
    form.addEventListener("submit", function (event) {
      if (!window.fetch) {
        return;
      }

      event.preventDefault();
      var button = form.querySelector("[data-favorite-button]");
      if (!button || button.disabled) {
        return;
      }

      var wasFavorite = form.getAttribute("data-is-favorite") === "true";
      var url = wasFavorite
        ? form.getAttribute("data-remove-url")
        : form.getAttribute("data-add-url");
      var body = new FormData(form);
      if (!body.get("__RequestVerificationToken")) {
        body.append("__RequestVerificationToken", getToken(form));
      }

      button.disabled = true;
      showStatus("");

      fetch(url, {
        method: "POST",
        body: body,
        headers: {
          Accept: "application/json",
          "X-Requested-With": "fetch"
        },
        credentials: "same-origin"
      })
        .then(function (response) {
          if (response.status === 401) {
            window.location.href =
              "/Account/SignIn?returnUrl=" + encodeURIComponent(window.location.pathname + window.location.search);
            return null;
          }

          return response
            .json()
            .catch(function () {
              return {};
            })
            .then(function (payload) {
              if (!response.ok) {
                throw new Error(
                  payload.message || "Your favourites could not be updated. Please try again."
                );
              }
              return payload;
            });
        })
        .then(function (payload) {
          if (!payload) {
            return;
          }

          var isFavorite = payload.isFavorite === true;
          render(form, isFavorite);
          showStatus(
            isFavorite ? "Saved to your favourites." : "Removed from your favourites.",
            false
          );
        })
        .catch(function (error) {
          render(form, wasFavorite);
          showStatus(error.message || "Your favourites could not be updated.", true);
        })
        .finally(function () {
          button.disabled = false;
        });
    });
  }

  document.addEventListener("DOMContentLoaded", function () {
    document.querySelectorAll("form.wishlist[data-favorite-toggle]").forEach(bindForm);
  });
})();
