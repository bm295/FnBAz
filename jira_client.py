import yaml
from jira import JIRA

class JiraClient:
    def __init__(self, cfg_path="config.yaml"):
        cfg = yaml.safe_load(open(cfg_path))
        j = cfg["jira"]
        self.jira = JIRA(server=j["url"],
                         basic_auth=(j["email"], j["api_token"]))

    def fetch_by_jql(self, jql):
        """Fetch all issues matching a full JQL (paginated), with changelog."""
        chunk, start, all_iss = 50, 0, []
        while True:
            block = self.jira.search_issues(
                jql,
                startAt=start,
                maxResults=chunk,
                expand="changelog"
            )
            if not block:
                break
            all_iss.extend(block)
            start += len(block)
        return all_iss

    def fetch_by_keys(self, keys):
        """Fetch individual issues by key, with changelog."""
        issues = []
        for key in keys:
            issues.append(
                self.jira.issue(key, expand="changelog")
            )
        return issues
