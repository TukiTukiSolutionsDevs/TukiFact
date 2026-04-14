import requests
import sys

BASE_URL = "http://localhost:80"
BACKOFFICE_LOGIN_URL = f"{BASE_URL}/v1/backoffice/auth/login"
MRR_REPORT_URL = f"{BASE_URL}/v1/backoffice/reports/mrr"
USAGE_REPORT_URL = f"{BASE_URL}/v1/backoffice/reports/usage"

BACKOFFICE_CREDENTIALS = {
    "email": "superadmin@tukifact.net.pe",
    "password": "SuperAdmin2026!"
}

def test_backoffice_reports_mrr_and_usage():
    try:
        # Authenticate backoffice user to get token
        login_resp = requests.post(BACKOFFICE_LOGIN_URL, json=BACKOFFICE_CREDENTIALS, timeout=30)
        assert login_resp.status_code == 200, f"Login failed with status {login_resp.status_code}"
        login_json = login_resp.json()
        access_token = login_json.get("accessToken")
        assert access_token and isinstance(access_token, str) and access_token.startswith("eyJ"), "Invalid backoffice accessToken"
        assert "user" in login_json and login_json["user"].get("role") == "superadmin", "User role not superadmin"

        headers = {
            "Authorization": f"Bearer {access_token}"
        }

        # GET /v1/backoffice/reports/mrr
        mrr_resp = requests.get(MRR_REPORT_URL, headers=headers, timeout=30)
        assert mrr_resp.status_code == 200, f"MRR report failed with status {mrr_resp.status_code}"
        mrr_json = mrr_resp.json()
        # Validate fields
        assert "totalMrr" in mrr_json, "'totalMrr' missing in MRR report"
        assert isinstance(mrr_json["totalMrr"], (int, float)), "'totalMrr' not a number"
        assert "activeTenantCount" in mrr_json, "'activeTenantCount' missing in MRR report"
        assert isinstance(mrr_json["activeTenantCount"], (int, float)), "'activeTenantCount' not a number"
        assert "mrrByPlan" in mrr_json, "'mrrByPlan' missing in MRR report"
        assert isinstance(mrr_json["mrrByPlan"], list), "'mrrByPlan' is not an array"

        # GET /v1/backoffice/reports/usage
        usage_resp = requests.get(USAGE_REPORT_URL, headers=headers, timeout=30)
        assert usage_resp.status_code == 200, f"Usage report failed with status {usage_resp.status_code}"
        usage_json = usage_resp.json()
        # Validate fields
        for field in ("todayDocs", "weekDocs", "monthDocs"):
            assert field in usage_json, f"'{field}' missing in usage report"
            assert isinstance(usage_json[field], (int, float)), f"'{field}' not a number"

        assert "topTenants" in usage_json, "'topTenants' missing in usage report"
        assert isinstance(usage_json["topTenants"], list), "'topTenants' is not an array"

        assert "byType" in usage_json, "'byType' missing in usage report"
        assert isinstance(usage_json["byType"], list), "'byType' is not an array"

    except requests.RequestException as e:
        print(f"Request failed: {e}", file=sys.stderr)
        raise
    except AssertionError as e:
        print(f"Assertion failed: {e}", file=sys.stderr)
        raise

test_backoffice_reports_mrr_and_usage()