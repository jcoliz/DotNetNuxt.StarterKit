Feature: Selected set of pages load successfully

    As a developer
    I want to verify that the test infrastructure is working and that key pages load correctly

    One requirement of this test is that we not have any Background steps, which could complicate the test.
    The point of this test is to verify that the most basic functionality (loading pages) is working at all.
    So we want to keep the setup as minimal as possible while still covering the top-level scenarios.

    Scenario: Site loads
        When user launches the site
        Then page loaded ok

    Scenario: Weather forecasts shown
        When user visits the Weather page
        Then 5 forecasts are visible

    Scenario: App information shown
        When user visits the About page
        Then backend version is visible
        And frontend version is visible
        And both versions match

    Scenario Outline: Home page cards navigate to expected pages
        Given user is on the Home page
        When user selects the <card> card
        Then the user lands on the <page> page

        Examples:
            | card    | page    |
            | Weather | Weather |
            | Profile | Profile |
            | About   | About   |