import re
import pandas as pd
from underthesea import word_tokenize, pos_tag

def text_complexity_features(text: str) -> dict:
    """
    Compute linguistic complexity features for a Vietnamese text.
    """
    # split into sentences
    sentences = [s.strip() for s in re.split(r"[.?!]+", text) if s.strip()]
    num_sentences = max(len(sentences), 1)

    # tokenize Vietnamese text
    tokens = word_tokenize(text, format="text").split()
    num_tokens = max(len(tokens), 1)
    avg_token_len = sum(len(t) for t in tokens) / num_tokens
    unique_tokens = len(set(tokens))
    lexical_diversity = unique_tokens / num_tokens

    # POS tagging: count nouns vs verbs
    pos_tags = pos_tag(text)
    num_nouns = sum(1 for _, tag in pos_tags if tag.startswith("N"))
    num_verbs = sum(1 for _, tag in pos_tags if tag.startswith("V"))
    noun_ratio = num_nouns / num_tokens
    verb_ratio = num_verbs / num_tokens

    avg_sentence_len = num_tokens / num_sentences

    return {
        "num_sentences":     num_sentences,
        "num_tokens":        num_tokens,
        "avg_token_len":     avg_token_len,
        "lexical_diversity": lexical_diversity,
        "noun_ratio":        noun_ratio,
        "verb_ratio":        verb_ratio,
        "avg_sentence_len":  avg_sentence_len,
    }

def build_feature_table(issues) -> pd.DataFrame:
    """
    Turn a list of jira-python Issue objects into a DataFrame
    with both numeric and text-derived features, including the raw summary.
    """
    rows = []
    for issue in issues:
        summary = issue.fields.summary or ""

        # count status changes from changelog
        histories = issue.changelog.histories
        status_changes = sum(
            1
            for h in histories
            for i in h.items
            if getattr(i, "field", "") == "status"
        )

        # comments
        comments = issue.fields.comment.comments or []
        num_comments = len(comments)

        # total text length (summary + all comment bodies)
        text_len = len(summary) + sum(len(c.body or "") for c in comments)

        # original story points (customfield_10100)
        orig_sp = getattr(issue.fields, "customfield_10100", None) or 0

        # compute summary complexity features
        txt_feats = text_complexity_features(summary)

        # assemble row
        row = {
            "key":             issue.key,
            "summary":         summary,
            "summary_len":     len(summary),
            "text_len":        text_len,
            "status_changes":  status_changes,
            "num_comments":    num_comments,
            "orig_sp":         orig_sp,
            **txt_feats
        }
        rows.append(row)

    return pd.DataFrame(rows)
