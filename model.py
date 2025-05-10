import joblib
from sklearn.ensemble import RandomForestRegressor
from sklearn.model_selection import train_test_split
from sklearn.pipeline import Pipeline
from sklearn.compose import ColumnTransformer
from sklearn.preprocessing import StandardScaler
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.decomposition import TruncatedSVD
from underthesea import word_tokenize

MODEL_PATH = "sp_model.joblib"

def viet_tokenizer(text):
    """
    Tokenizer for Vietnamese text using underthesea.
    """
    # underthesea.word_tokenize returns a string with spaces between tokens
    return word_tokenize(text, format="text").split()

def train_model(df):
    # 1) Numeric feature names
    num_cols = [
        "summary_len",
        "text_len",
        "status_changes",
        "num_comments",
        "num_sentences",
        "num_tokens",
        "avg_token_len",
        "lexical_diversity",
        "noun_ratio",
        "verb_ratio",
        "avg_sentence_len",
    ]

    # 2) Text pipeline: TF-IDF → SVD
    text_pipeline = Pipeline([
        ("tfidf", TfidfVectorizer(
            tokenizer=viet_tokenizer,
            lowercase=True,
            ngram_range=(1, 2),
            max_features=5000
        )),
        ("svd", TruncatedSVD(n_components=50, random_state=42)),
    ])

    # 3) Preprocessor: scale numeric, transform summary text
    preprocessor = ColumnTransformer(
        transformers=[
            ("num", StandardScaler(), num_cols),
            ("txt", text_pipeline, "summary"),
        ],
        remainder="drop"  # drop other columns like 'key' or 'orig_sp'
    )

    # 4) Full pipeline: preprocessing → regressor
    pipeline = Pipeline([
        ("preprocessor", preprocessor),
        ("regressor", RandomForestRegressor(n_estimators=100, random_state=42)),
    ])

    # Prepare X (features) and y (target)
    X = df.drop(columns=["key", "orig_sp"])
    y = df["orig_sp"]

    # Train/test split if dataset is large enough
    if len(df) > 1:
        X_train, X_val, y_train, y_val = train_test_split(
            X, y, test_size=0.25, random_state=42
        )
        pipeline.fit(X_train, y_train)
        print("✅ Validation R² score:", pipeline.score(X_val, y_val))
    else:
        print("⚠️ Only one sample—training on all data.")
        pipeline.fit(X, y)

    # Persist the entire pipeline
    joblib.dump(pipeline, MODEL_PATH)
    return pipeline

def load_model():
    return joblib.load(MODEL_PATH)
