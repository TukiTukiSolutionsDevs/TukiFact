import requests

BASE_URL = "http://localhost:5186"
LOGIN_PATH = "/v1/auth/login"
WEBHOOKS_PATH = "/v1/webhooks"
LOGIN_PAYLOAD = {
    "email": "admin@tukitest.pe",
    "password": "TestSprite2026!",
    "tenantId": "0c700fd7-1fdb-43d5-a2a8-d20fe4bcab84"
}
TIMEOUT = 30


def test_webhooks_full_lifecycle():
    # Step 1: Login and get access token
    login_resp = requests.post(
        BASE_URL + LOGIN_PATH,
        json=LOGIN_PAYLOAD,
        headers={"Content-Type": "application/json"},
        timeout=TIMEOUT
    )
    assert login_resp.status_code == 200, f"Login failed: {login_resp.text}"
    login_data = login_resp.json()
    access_token = login_data.get("accessToken")
    assert access_token and isinstance(access_token, str), "Missing or invalid accessToken"

    headers = {
        "Authorization": f"Bearer {access_token}",
        "Content-Type": "application/json"
    }

    webhook_id = None

    try:
        # Step 2: GET /v1/webhooks, expect 200 OK
        get_webhooks_resp = requests.get(
            BASE_URL + WEBHOOKS_PATH,
            headers=headers,
            timeout=TIMEOUT
        )
        assert get_webhooks_resp.status_code == 200, f"GET /webhooks failed: {get_webhooks_resp.text}"

        # Step 3: POST /v1/webhooks to create a new webhook
        post_body = {
            "url": "https://httpbin.org/post",
            "events": ["document.created", "document.accepted"],
            "maxRetries": 3
        }
        post_resp = requests.post(
            BASE_URL + WEBHOOKS_PATH,
            headers=headers,
            json=post_body,
            timeout=TIMEOUT
        )
        assert post_resp.status_code == 201, f"POST /webhooks failed: {post_resp.text}"
        post_data = post_resp.json()
        webhook_id = post_data.get("id")
        secret = post_data.get("secret")
        assert webhook_id and isinstance(webhook_id, str), "Missing or invalid webhook id"
        assert secret and isinstance(secret, str), "Missing or invalid webhook secret"

        # Step 4: PUT /v1/webhooks/{id} to deactivate webhook
        put_resp = requests.put(
            f"{BASE_URL}{WEBHOOKS_PATH}/{webhook_id}",
            headers=headers,
            json={"isActive": False},
            timeout=TIMEOUT
        )
        assert put_resp.status_code == 204, f"PUT /webhooks/{webhook_id} failed: {put_resp.text}"

        # Step 5: GET /v1/webhooks/{id}/deliveries, expect JSON array
        deliveries_resp = requests.get(
            f"{BASE_URL}{WEBHOOKS_PATH}/{webhook_id}/deliveries",
            headers=headers,
            timeout=TIMEOUT
        )
        assert deliveries_resp.status_code == 200, f"GET /webhooks/{webhook_id}/deliveries failed: {deliveries_resp.text}"
        deliveries_data = deliveries_resp.json()
        assert isinstance(deliveries_data, list), "Deliveries response is not a JSON array"

    finally:
        # Step 6: DELETE /v1/webhooks/{id}
        if webhook_id:
            delete_resp = requests.delete(
                f"{BASE_URL}{WEBHOOKS_PATH}/{webhook_id}",
                headers=headers,
                timeout=TIMEOUT
            )
            assert delete_resp.status_code == 204, f"DELETE /webhooks/{webhook_id} failed: {delete_resp.text}"


test_webhooks_full_lifecycle()