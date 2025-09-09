# How to Run

## Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) installed

## Run the Application
1. Configure settings in `appsettings.json`.
2. Open a terminal (like Command Prompt or PowerShell) in the output directory.
3. In the terminal, execute the application by typing `ValkyrieAutoTranslator.exe` and pressing Enter.

Alternatively, you can navigate to the output directory in Windows File Explorer and double-click on `ValkyrieAutoTranslator.exe` to run it.

# Settings
For detailed information on configuring the application, please refer to appsettings.json.

# Run modes
You can run the application with different run modes:
1. You can decide if you want to use AI translation features (Azure or DeepL => useLlmApi = true).
2. Optionally you can run additional AI large language model logic (e.g. DeepSeck, useLlmApi = true).
3. Even when both modes are set to false you can run a dry run that will apply fixes to the localization file:
   1. Replace double quotes with correct quotes of the target language (e.g. "Quote text" becomes „Quote text“ in german).
   1. Replace all quotes at start of a line with ||| and ensure that lines starting ||| also end with it.
   1. Ensures that if a line starts with pipes | than it always starts with three pipes.

All changes will always be made line by line of the source file (also API calls will be made line by line).

# DeepL API Integration

This application supports both the **DeepL API Free** and **DeepL API Pro** plans. The choice between them is controlled via the `appsettings.json` file.

## Configuration

1.  **Set Provider**: Set `"translatorProvider"` to `"DeepL"`.
2.  **Add API Key**: Place your API key in `"deepLApiKey"`.
3.  **Choose API Mode**: Set `"deepLApiMode"` to `"free"` or `"paid"` based on your subscription.
4. Set secret for DeepL API.

    ```json
    "secrets": {
        "deepLApiKey": "YOUR_API_KEY"
    },
    "translation": {
      "translatorProvider": "DeepL",
      "deepL": {
        "deepLApiMode": "free"
      }
    }
    ```
## Formality
You can control the level of formality for the translation by setting `"deepLFormality"` to `"more"` or `"less"` for supported languages.

For more details, refer to the official DeepL API documentation.

## DeepL Glossary Usage

The application can leverage DeepL's Glossary feature to enforce specific translations for predefined terms. This is managed through a local CSV file.

Specify the file location using parameters

  ```json
    "secrets": {
          "deepLApiKey": "YOUR_API_KEY"
      },
    "translation": {
      "translatorProvider": "DeepL",
      "deepL": {
        "deepLApiUpdateGlossary": "true",
        "deepLGlossaryFilePath": "PathToYourFile"
      }
    }
  ```

## How it Works

1.  **Glossary File**: You provide a CSV file containing pairs of terms in the source and target languages. The application reads this file to build the glossary.
2.  **Automatic Updates**: When the `deepLApiUpdateGlossary` setting is enabled (`true`), the application performs the following steps at startup:
    *   It connects to your DeepL account.
    *   It **deletes all existing glossaries** to ensure a clean slate.
    *   It creates a **new glossary** named `ValkyrieGlossary` using the entries from your local CSV file.
    *   This new glossary is then used for all subsequent translations in the current run.
3.  **Using an Existing Glossary**: If `deepLApiUpdateGlossary` is set to `false`, the application will simply search for the first available glossary in your DeepL account and use it for translations.

## Configuration

To enable the glossary feature, configure the following in `appsettings.json`:

1.  **Enable Translation**: Make sure `"translate"` is `"true"` and `"translatorProvider"` is `"DeepL"`.
2.  **Glossary File Path**: Provide the full path to your glossary CSV file in `"glossaryFilePath"`.
3.  **Column Names**: Ensure the `"sourceLanguageName"` and `"targetLanguageName"` settings match the column headers in your CSV file (e.g., "English" and "German").
4.  **Update Mode**: Set `"deepLApiUpdateGlossary"` to `"true"` to automatically update the glossary from your file on every run. Set it to `"false"` to use an existing glossary.

**Example `appsettings.json` configuration:**

```json
"translation": {
  "glossaryFilePath": "E:\\Path\\To\\Your\\Glossary.csv",
  "deepL": {
    "deepLApiUpdateGlossary": "true"
  }
}
```

# Azure API Integration

The application can use Microsoft's Azure Translator service for its translation needs. You can use [custom translation models](https://learn.microsoft.com/bg-bg/azure/ai-services/Translator/custom-translator/how-to/translate-with-custom-model) by specifying `azureCategoryId`.

  ```json
    "secrets": {
          "azureAuthentificationKey": "YOUR_API_KEY"
      },
    "translation": {
      "translatorProvider": "Azure",
      "azure": {
        "azureCategoryId": "YOUR_CUSTOM_MODEL_ID"
      }
    }
  ```

## Configuration

To use the Azure Translator API, you need to configure the following settings in your `appsettings.json` file:

1.  **Set the Translator Provider**:
    *   Ensure `"translatorProvider"` is set to `"Azure"`.

2.  **Provide your API Key**:
    *   Place your Azure Translator resource key in the `"azureAuthentificationKey"` field within the `"secrets"` section.

3.  **(Optional) Use a Custom Translation Model**:
    *   If you have a custom translation model trained with Custom Translator, you can specify its Category ID in the `"azureCategoryId"` field. This ID tells Azure to use your specific model instead of the general-purpose one.
    *   If you leave this field empty, the standard, general-purpose translation model will be used.

**Example `appsettings.json` configuration:**

```json
"translation": {
  "translate": "true",
  "translatorProvider": "Azure",
  "azure": {
    "azureCategoryId": "YOUR_CUSTOM_MODEL_ID"
  }
},
"secrets": {
    "azureAuthentificationKey": "YOUR_AZURE_TRANSLATOR_KEY"
}
```

# DeepSeck API Integration
The application can leverage the DeepSeek API to use a Large Language Model (LLM) for text generation and refinement. This can be used to improve the quality of translations or even act as the sole translation engine.

## How it Works
The DeepSeek integration is controlled by the `useLlmApi` setting. It can operate in two modes depending on the translate setting:

### Enhancement Mode (translate: true, useLlmApi: true):
First, the text is translated by the primary translation provider (DeepL or Azure). Then, the translated text is passed to the DeepSeek API along with a custom prompt (llmPrompt). The LLM refines the translation, which can improve fluency, tone, and context.

###  Standalone Mode (translate: false, useLlmApi: true):
The primary translation provider is skipped. The original source text is sent directly to the DeepSeek API with your custom prompt. In this mode, your prompt must instruct the LLM to perform the translation (e.g., "Translate the following English text to German...").

## Configuration
To use the DeepSeek API, configure the following in appsettings.json:
1. Enable LLM: Set "useLlmApi" to "true".
2. Provide API Key: Place your DeepSeek API key in the "deepSeekApiKey" field within the "secrets" section.
3. Define a Prompt: Write a clear instruction for the LLM in the "llmPrompt" field. This prompt is critical for getting the desired output.

Example appsettings.json for Enhancement Mode:

```json:
{
"useLlmApi": "true",
"llmPrompt": "You are an expert translator. Refine the following translation to make it sound more natural and fluent in German, while preserving the original meaning." }
"secrets": {
    "deepSeekApiKey": "YOUR_DEEPSEEK_API_KEY"
}
```


# Cache
The application uses a local file-based cache to store translations and avoid re-translating the same text, which saves API costs and speeds up subsequent runs.

## How it Works
1.  **Configuration**: Caching is enabled by providing a path in the `translationCacheFilePath` setting in `appsettings.json`. If the path is empty, caching is disabled.
2.  **Cache File**: A file named `ValkyrieTranslationCache.csv` is created in the specified directory. This file stores key-value pairs, where the `Key` is the original source text and the `Value` is the translated text.
3.  **Process**:
    - Before translating a line of text, the application checks if the source text exists as a `Key` in the cache file.
    - **Cache Hit**: If the text is found, the corresponding translated `Value` is used directly, and the API call is skipped.
    - **Cache Miss**: If the text is not found, it is translated via the configured API (DeepL/Azure). The new translation pair (original text and translated text) is then added to the cache.
4.  **Saving**: At the end of the run, the in-memory cache (including any new translations) is written back to the `ValkyrieTranslationCache.csv` file, making it available for future runs.
