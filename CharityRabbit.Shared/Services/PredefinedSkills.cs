namespace CharityRabbit.Data;

/// <summary>
/// The static skill catalog, shared so both the server (DB seeding, suggestion fallbacks)
/// and the mobile app (offline suggestion fallbacks) read the same data without a network call.
/// </summary>
public static class PredefinedSkills
{
    public static Dictionary<string, List<string>> All => new()
    {
        ["Physical"] = new() { "Manual Labor", "Lifting & Moving", "Construction", "Gardening", "Cleaning", "Painting", "Landscaping", "Driving", "Sports & Fitness" },
        ["Technical"] = new() { "Computer Skills", "Web Development", "Graphic Design", "Video Editing", "Photography", "Social Media Management", "Data Entry", "IT Support", "Software Development" },
        ["Social"] = new() { "Public Speaking", "Teaching & Tutoring", "Customer Service", "Event Planning", "Team Leadership", "Mentoring", "Counseling", "Networking", "Community Outreach" },
        ["Creative"] = new() { "Writing & Editing", "Arts & Crafts", "Music", "Cooking & Baking", "Event Decoration", "Content Creation", "Marketing", "Storytelling", "Design Thinking" },
        ["Administrative"] = new() { "Organization", "Scheduling", "Record Keeping", "Phone Skills", "Email Management", "Bookkeeping", "Project Management", "Filing & Documentation", "Office Management" },
        ["Healthcare"] = new() { "First Aid", "CPR Certified", "Medical Knowledge", "Elderly Care", "Child Care", "Mental Health Support", "Nutrition", "Physical Therapy", "Patient Care" },
        ["Language"] = new() { "Spanish", "French", "Mandarin", "Sign Language", "Translation", "Multilingual", "ESL Teaching", "Interpretation" },
        ["Specialized"] = new() { "Legal Knowledge", "Financial Planning", "Fundraising", "Grant Writing", "Research", "Environmental Science", "Animal Care", "Emergency Response", "Disaster Relief" },
    };
}
