import { readFileSync, existsSync } from 'fs';
import { join } from 'path';

/**
 * User Prompt Submit Hook - Auto-activates skills based on prompt content
 *
 * This hook runs BEFORE Claude sees your prompt and checks if any skills
 * should be activated based on keywords, intent patterns, or file references.
 */

interface SkillRule {
  type: string;
  enforcement: string;
  priority: string;
  description: string;
  promptTriggers?: {
    keywords?: string[];
    intentPatterns?: string[];
  };
  fileTriggers?: {
    pathPatterns?: string[];
    contentPatterns?: string[];
  };
}

interface SkillRules {
  [skillName: string]: SkillRule;
}

export default async function userPromptSubmit(params: any): Promise<string> {
  try {
    const { prompt, workingDirectory } = params;

    if (!prompt || typeof prompt !== 'string') {
      return prompt;
    }

    // Read skill-rules.json
    const rulesPath = join(workingDirectory, '.claude', 'skill-rules.json');
    if (!existsSync(rulesPath)) {
      return prompt;
    }

    const rulesContent = readFileSync(rulesPath, 'utf-8');
    const rules: SkillRules = JSON.parse(rulesContent);

    // Check each skill for activation
    const activatedSkills: string[] = [];

    for (const [skillName, config] of Object.entries(rules)) {
      if (shouldActivateSkill(prompt, config)) {
        activatedSkills.push(skillName);
      }
    }

    // If skills should be activated, prepend reminder
    if (activatedSkills.length > 0) {
      const reminder = buildActivationReminder(activatedSkills, rules);
      return reminder + '\n\n' + prompt;
    }

    return prompt;

  } catch (error) {
    // Fail gracefully - never break the prompt submission
    console.error('Error in user-prompt-submit hook:', error);
    return params.prompt;
  }
}

/**
 * Check if a skill should be activated based on prompt content
 */
function shouldActivateSkill(prompt: string, config: SkillRule): boolean {
  const promptLower = prompt.toLowerCase();

  // Check keywords
  if (config.promptTriggers?.keywords) {
    for (const keyword of config.promptTriggers.keywords) {
      if (promptLower.includes(keyword.toLowerCase())) {
        return true;
      }
    }
  }

  // Check intent patterns (regex)
  if (config.promptTriggers?.intentPatterns) {
    for (const pattern of config.promptTriggers.intentPatterns) {
      try {
        const regex = new RegExp(pattern, 'i');
        if (regex.test(prompt)) {
          return true;
        }
      } catch (e) {
        // Invalid regex, skip
        continue;
      }
    }
  }

  // Check for .cs file mentions in prompt
  if (config.fileTriggers?.pathPatterns) {
    for (const pathPattern of config.fileTriggers.pathPatterns) {
      // Convert glob pattern to simple check
      if (pathPattern.includes('**/*.cs') && /\.cs\b/i.test(prompt)) {
        return true;
      }
      if (pathPattern.includes('Brain') && /Brain/i.test(prompt)) {
        return true;
      }
    }
  }

  return false;
}

/**
 * Build the activation reminder message
 */
function buildActivationReminder(skillNames: string[], rules: SkillRules): string {
  if (skillNames.length === 1) {
    const skillName = skillNames[0];
    const config = rules[skillName];

    return `━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
🎯 SKILL ACTIVATION CHECK
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

💡 Consider using the "${skillName}" skill for:
   - Code formatting and naming standards
   - Brain architecture patterns (singleton, state machine)
   - MonoBehaviour lifecycle best practices
   - Object pooling patterns

Use: /skill ${skillName}
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━`;
  } else {
    // Multiple skills (unlikely with current setup, but handle it)
    const skillList = skillNames.map(name => `   - ${name}`).join('\n');
    return `━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
🎯 SKILL ACTIVATION CHECK
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

💡 Consider using these skills:
${skillList}
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━`;
  }
}
