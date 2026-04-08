import requests

BASE_URL = "http://localhost:5186"
TIMEOUT = 30

def test_get_v1_plans_list_active_subscription_plans():
    url = f"{BASE_URL}/v1/plans"
    try:
        response = requests.get(url, timeout=TIMEOUT)
    except requests.RequestException as e:
        assert False, f"Request failed: {e}"

    assert response.status_code == 200, f"Expected status 200 but got {response.status_code}"

    try:
        plans = response.json()
    except ValueError:
        assert False, "Response is not valid JSON"

    assert isinstance(plans, list), "Response JSON is not a list"

    for plan in plans:
        assert isinstance(plan, dict), "Each plan should be a dictionary"
        assert "name" in plan and isinstance(plan["name"], str) and plan["name"], "Plan 'name' missing or invalid"
        assert "priceMonthly" in plan and (isinstance(plan["priceMonthly"], (int, float))), "Plan 'priceMonthly' missing or invalid"
        assert "maxDocumentsPerMonth" in plan and isinstance(plan["maxDocumentsPerMonth"], int), "Plan 'maxDocumentsPerMonth' missing or invalid"
        assert "features" in plan and isinstance(plan["features"], dict) and plan["features"] is not None, "Plan 'features' missing, null, or not an object"

test_get_v1_plans_list_active_subscription_plans()