import { RouterProvider } from 'react-router-dom';
import { router } from '@/router';

/**
 * App.jsx — Root component.
 *
 * Beginner: This is the TOP of the React tree.
 * Everything in our app lives inside <App />.
 * We only put the Router here — all pages are defined in /router/index.jsx
 */
export default function App() {
  return <RouterProvider router={router} />;
}
