import requests

BASE_URL = "http://localhost:5186"
LOGIN_ENDPOINT = "/v1/auth/login"
DASHBOARD_ENDPOINT = "/v1/dashboard"
TIMEOUT = 30

LOGIN_PAYLOAD = {
    "email": "admin@tukitest.pe",
    "password": "TestSprite2026!",
    "tenantId": "0c700fd7-1fdb-43d5-a2a8-d20fe4bcab84"
}


def test_dashboard_returns_kpis():
    # Step 1: Login to obtain Bearer token
    try:
        login_resp = requests.post(
            BASE_URL + LOGIN_ENDPOINT,
            json=LOGIN_PAYLOAD,
            headers={"Content-Type": "application/json"},
            timeout=TIMEOUT,
        )
        login_resp.raise_for_status()
    except requests.RequestException as e:
        assert False, f"Login request failed: {e}"
    login_json = login_resp.json()
    assert "accessToken" in login_json and isinstance(login_json["accessToken"], str) and login_json["accessToken"], "No valid accessToken in login response"
    token = login_json["accessToken"]

    headers = {
        "Authorization": f"Bearer {token}",
        "Content-Type": "application/json",
        "Accept": "application/json",
    }

    # Step 2: GET /v1/dashboard with Bearer token
    try:
        resp = requests.get(BASE_URL + DASHBOARD_ENDPOINT, headers=headers, timeout=TIMEOUT)
        resp.raise_for_status()
    except requests.RequestException as e:
        assert False, f"Dashboard request failed: {e}"

    assert resp.status_code == 200, f"Expected 200 OK, got {resp.status_code}"

    try:
        data = resp.json()
    except ValueError:
        assert False, "Response is not valid JSON"

    # Validate presence and type of expected fields
    # 'today' object with 'totalAmount' and 'totalDocuments'
    assert "today" in data and isinstance(data["today"], dict), "'today' object missing or invalid"
    today = data["today"]
    assert "totalAmount" in today, "'totalAmount' missing in today"
    assert isinstance(today["totalAmount"], (int, float)), "'totalAmount' in today is not a number"
    assert "totalDocuments" in today, "'totalDocuments' missing in today"
    assert isinstance(today["totalDocuments"], int), "'totalDocuments' in today is not an integer"

    # 'thisMonth' object present
    assert "thisMonth" in data and isinstance(data["thisMonth"], dict), "'thisMonth' object missing or invalid"

    # 'byType' array present
    assert "byType" in data and isinstance(data["byType"], list), "'byType' array missing or invalid"

    # 'byStatus' array present
    assert "byStatus" in data and isinstance(data["byStatus"], list), "'byStatus' array missing or invalid"

    # 'monthlySales' array present
    assert "monthlySales" in data and isinstance(data["monthlySales"], list), "'monthlySales' array missing or invalid"


test_dashboard_returns_kpis()