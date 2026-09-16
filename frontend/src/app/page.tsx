import { SiteHeader } from "@/components/shared/site-header";
import { SiteFooter } from "@/components/shared/site-footer";
import { Hero } from "@/components/landing/hero";
import { WhatIs } from "@/components/landing/what-is";
import { Journey } from "@/components/landing/journey";
import { FeaturedCourses } from "@/components/landing/featured-courses";
import { HowItWorks } from "@/components/landing/how-it-works";
import { OutcomesCta } from "@/components/landing/outcomes-cta";

// Landing storytelling order (UiUxDesign §10):
// Hero → What is EduCore → Journey → Featured courses →
// How it works → Outcomes/CTA → Footer
export default function HomePage() {
  return (
    <div className="flex min-h-svh flex-col">
      <SiteHeader />
      <main className="flex-1">
        <Hero />
        <WhatIs />
        <Journey />
        <FeaturedCourses />
        <HowItWorks />
        <OutcomesCta />
      </main>
      <SiteFooter />
    </div>
  );
}
