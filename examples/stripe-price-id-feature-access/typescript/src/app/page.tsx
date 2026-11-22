export default function Home() {
  return <main>
    <h1>FeedbackDesk price mapping demo</h1>
    <p>Each request uses a fixed demo customer. The API checks the stable csv-export feature.</p>
    <ul>
      <li><a href="/api/demo/export/legacy-team">Legacy Studio customer</a></li>
      <li><a href="/api/demo/export/current-team">Current Studio customer</a></li>
      <li><a href="/api/demo/export/free-team">Free customer</a></li>
    </ul>
  </main>;
}
