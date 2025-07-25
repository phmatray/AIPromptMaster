using AIPromptManager.Models;
using Microsoft.EntityFrameworkCore;

namespace AIPromptManager.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(PromptManagerContext context)
    {
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // Check if data already exists
        if (await context.Prompts.AnyAsync() || await context.Tags.AnyAsync())
        {
            return; // Database has been seeded
        }

        // Create sample tags
        var tags = new List<Tag>
        {
            new Tag { Name = "Writing", CreatedAt = DateTime.UtcNow },
            new Tag { Name = "Code", CreatedAt = DateTime.UtcNow },
            new Tag { Name = "Analysis", CreatedAt = DateTime.UtcNow },
            new Tag { Name = "Creative", CreatedAt = DateTime.UtcNow },
            new Tag { Name = "Business", CreatedAt = DateTime.UtcNow },
            new Tag { Name = "Technical", CreatedAt = DateTime.UtcNow },
            new Tag { Name = "Research", CreatedAt = DateTime.UtcNow },
            new Tag { Name = "Marketing", CreatedAt = DateTime.UtcNow }
        };

        await context.Tags.AddRangeAsync(tags);
        await context.SaveChangesAsync();

        // Create sample prompts
        var prompts = new List<Prompt>
        {
            new Prompt
            {
                Title = "Code Review Assistant",
                Description = "A prompt for reviewing code and providing constructive feedback",
                Content = "Please review the following code and provide feedback on:\n1. Code quality and best practices\n2. Potential bugs or issues\n3. Performance improvements\n4. Readability and maintainability\n\nCode to review:\n[INSERT CODE HERE]",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Tags = new List<Tag> { tags[1], tags[5] } // Code, Technical
            },
            new Prompt
            {
                Title = "Blog Post Writer",
                Description = "Generate engaging blog posts on various topics",
                Content = "Write a comprehensive blog post about [TOPIC]. The post should:\n- Be 800-1200 words long\n- Include an engaging introduction\n- Have clear headings and subheadings\n- Provide practical examples\n- End with a compelling conclusion\n- Be optimized for SEO\n\nTopic: [INSERT TOPIC HERE]",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Tags = new List<Tag> { tags[0], tags[3], tags[7] } // Writing, Creative, Marketing
            },
            new Prompt
            {
                Title = "Data Analysis Helper",
                Description = "Analyze datasets and provide insights",
                Content = "Analyze the following dataset and provide:\n1. Summary statistics\n2. Key trends and patterns\n3. Potential correlations\n4. Actionable insights\n5. Recommendations for further analysis\n\nDataset description: [DESCRIBE DATASET]\nData: [INSERT DATA HERE]",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Tags = new List<Tag> { tags[2], tags[5], tags[6] } // Analysis, Technical, Research
            },
            new Prompt
            {
                Title = "Meeting Summary Generator",
                Description = "Create structured summaries from meeting notes",
                Content = "Create a professional meeting summary from the following notes:\n\n**Meeting Summary Template:**\n- Date and Attendees\n- Key Discussion Points\n- Decisions Made\n- Action Items (with owners and deadlines)\n- Next Steps\n\nMeeting Notes:\n[INSERT MEETING NOTES HERE]",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Tags = new List<Tag> { tags[4], tags[0] } // Business, Writing
            },
            new Prompt
            {
                Title = "Creative Story Starter",
                Description = "Generate creative story beginnings and plot ideas",
                Content = "Create an engaging story beginning based on the following elements:\n- Genre: [SPECIFY GENRE]\n- Setting: [DESCRIBE SETTING]\n- Main character: [CHARACTER DESCRIPTION]\n- Conflict/Challenge: [DESCRIBE CONFLICT]\n\nWrite a compelling opening paragraph that hooks the reader and sets up the story. Include vivid descriptions and establish the tone.",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Tags = new List<Tag> { tags[3], tags[0] } // Creative, Writing
            },
            new Prompt
            {
                Title = "API Documentation Generator",
                Description = "Create comprehensive API documentation",
                Content = "Generate detailed API documentation for the following endpoint:\n\n**Documentation should include:**\n- Endpoint URL and HTTP method\n- Description and purpose\n- Request parameters (path, query, body)\n- Request/response examples\n- Error codes and messages\n- Authentication requirements\n\nAPI Details:\n[INSERT API INFORMATION HERE]",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Tags = new List<Tag> { tags[1], tags[5], tags[0] } // Code, Technical, Writing
            },
            new Prompt
            {
                Title = "Market Research Analyst",
                Description = "Conduct market analysis and competitive research",
                Content = "Conduct a market analysis for [PRODUCT/SERVICE] including:\n\n1. **Market Size and Growth**\n   - Total addressable market\n   - Growth trends and projections\n\n2. **Competitive Landscape**\n   - Key competitors\n   - Their strengths and weaknesses\n   - Market positioning\n\n3. **Target Audience**\n   - Demographics and psychographics\n   - Pain points and needs\n\n4. **Opportunities and Threats**\n   - Market gaps\n   - Potential challenges\n\nProduct/Service: [INSERT DETAILS HERE]",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Tags = new List<Tag> { tags[4], tags[6], tags[2] } // Business, Research, Analysis
            },
            new Prompt
            {
                Title = "SQL Query Optimizer",
                Description = "Optimize SQL queries for better performance",
                Content = "Analyze and optimize the following SQL query:\n\n**Original Query:**\n[INSERT SQL QUERY HERE]\n\n**Please provide:**\n1. Performance analysis of the current query\n2. Optimized version of the query\n3. Explanation of improvements made\n4. Index recommendations if applicable\n5. Alternative approaches if relevant\n\n**Database Schema Context:**\n[DESCRIBE RELEVANT TABLES AND RELATIONSHIPS]",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Tags = new List<Tag> { tags[1], tags[5], tags[2] } // Code, Technical, Analysis
            }
        };

        await context.Prompts.AddRangeAsync(prompts);
        await context.SaveChangesAsync();
    }
}