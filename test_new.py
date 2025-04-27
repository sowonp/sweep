# import pandas as pd
# import random
# import heapq
# import numpy as np
# import time
# import os
# import glob

# # ================================================
# # 작업 디렉토리 설정
# # ================================================
# os.chdir("C:/")
# os.makedirs("algo_new_test_results", exist_ok=True)

# # ================================================
# # CSV 데이터 로드
# # ================================================
# def load_data(file_path):
#     return pd.read_csv(file_path)

# data = load_data('2023.csv')

# # ================================================
# # 환경 기반 쓰레기 및 장애물 생성 함수
# # ================================================
# def generate_trash_obstacles_with_environment(grid_size, env_row):
#     # === 환경 데이터 읽기 ===
#     wind_speed = env_row['풍속(m/s)']
#     wind_direction = env_row['풍향(deg)']
#     gust_speed = env_row['GUST풍속(m/s)']
#     max_wave_height = env_row['최대파고(m)']
#     significant_wave_height = env_row['유의파고(m)']
#     mean_wave_height = env_row['평균파고(m)']
#     wave_period = env_row['파주기(sec)']
#     wave_direction = env_row['파향(deg)']

#     # === NaN 처리 (기본값 설정) ===
#     if pd.isna(wind_speed):
#         wind_speed = 0
#     if pd.isna(wind_direction):
#         wind_direction = 0
#     if pd.isna(gust_speed):
#         gust_speed = 0
#     if pd.isna(max_wave_height):
#         max_wave_height = 0
#     if pd.isna(significant_wave_height):
#         significant_wave_height = 0
#     if pd.isna(mean_wave_height):
#         mean_wave_height = 0
#     if pd.isna(wave_period):
#         wave_period = 0
#     if pd.isna(wave_direction):
#         wave_direction = 0

#     # === 쓰레기와 장애물 개수 결정 ===
#     base_trash = 50
#     base_obstacles = 10

#     trash_count = base_trash + int(wind_speed * 5) + int(significant_wave_height * 10)
#     obstacle_count = base_obstacles + int(gust_speed * 2) + int(max_wave_height * 5)

#     trash_count = min(trash_count, 300)
#     obstacle_count = min(obstacle_count, 50)

#     # === 쓰레기 배치 (바람 방향 고려) ===
#     trash_positions = []
#     for _ in range(trash_count):
#         base_x = random.randint(0, grid_size-1)
#         base_y = random.randint(0, grid_size-1)
        
#         offset_distance = random.randint(0, 5)
#         offset_x = int(np.cos(np.radians(wind_direction)) * offset_distance)
#         offset_y = int(np.sin(np.radians(wind_direction)) * offset_distance)

#         new_x = max(0, min(grid_size-1, base_x + offset_x))
#         new_y = max(0, min(grid_size-1, base_y + offset_y))
        
#         trash_positions.append((new_x, new_y))

#     # === 장애물 배치 (파향 고려) ===
#     obstacle_positions = []
#     for _ in range(obstacle_count):
#         base_x = random.randint(0, grid_size-1)
#         base_y = random.randint(0, grid_size-1)
        
#         offset_distance = random.randint(0, 3)
#         offset_x = int(np.cos(np.radians(wave_direction)) * offset_distance)
#         offset_y = int(np.sin(np.radians(wave_direction)) * offset_distance)

#         new_x = max(0, min(grid_size-1, base_x + offset_x))
#         new_y = max(0, min(grid_size-1, base_y + offset_y))

#         obstacle_positions.append((new_x, new_y))

#     return trash_positions, obstacle_positions

# # ================================================
# # 퓨전 알고리즘
# # ================================================
# import heapq
# import numpy as np

# def fusion_algo(start, trash_positions, obstacle_positions):
#     grid_size = 50
#     directions = [(-1,0), (1,0), (0,-1), (0,1)]
#     path = []
#     current_pos = start

#     def heuristic(a, b):
#         return np.linalg.norm(np.array(a) - np.array(b))

#     def a_star(start, goal, obstacles):
#         open_set = []
#         heapq.heappush(open_set, (0 + heuristic(start, goal), 0, start, [start]))
#         visited = set()

#         while open_set:
#             est_total_cost, cost_so_far, current, current_path = heapq.heappop(open_set)

#             if current == goal:
#                 return current_path

#             if current in visited:
#                 continue

#             visited.add(current)

#             for d in directions:
#                 neighbor = (current[0] + d[0], current[1] + d[1])
#                 if (0 <= neighbor[0] < grid_size and 0 <= neighbor[1] < grid_size and neighbor not in obstacles):
#                     if neighbor not in visited:
#                         new_cost = cost_so_far + 1
#                         est_total = new_cost + heuristic(neighbor, goal)
#                         heapq.heappush(open_set, (est_total, new_cost, neighbor, current_path + [neighbor]))
#         return None

#     while trash_positions:
#         # 가장 가까운 쓰레기 선택 (Greedy)
#         next_trash = min(trash_positions, key=lambda p: heuristic(current_pos, p))

#         # 경로 탐색 (A*)
#         route = a_star(current_pos, next_trash, set(obstacle_positions))

#         # 만약 경로를 못 찾으면 Dijkstra 스타일로 brute-force
#         if route is None:
#             candidates = []
#             for trash in trash_positions:
#                 route_try = a_star(current_pos, trash, set(obstacle_positions))
#                 if route_try:
#                     candidates.append((len(route_try), trash, route_try))
#             if candidates:
#                 candidates.sort()
#                 _, next_trash, route = candidates[0]
#             else:
#                 trash_positions.remove(next_trash)
#                 continue

#         # 경로 추가
#         path.extend(route[1:])
#         current_pos = next_trash
#         trash_positions.remove(next_trash)

#         # 주변 센싱 (D*처럼 재탐색)
#         nearby_trashes = [p for p in trash_positions if heuristic(current_pos, p) <= 5]
#         if nearby_trashes:
#             for p in nearby_trashes:
#                 route_to_nearby = a_star(current_pos, p, set(obstacle_positions))
#                 if route_to_nearby:
#                     path.extend(route_to_nearby[1:])
#                     current_pos = p
#                     trash_positions.remove(p)

#     return path

# # ================================================
# # 예측 알고리즘
# # ================================================
# def predictive_algo(start, trash_positions, obstacle_positions, lookahead=2):
#     """
#     Predictive Greedy A* Algorithm
#     start: 시작 위치 (x, y)
#     trash_positions: 수거해야할 쓰레기 리스트 [(x,y), (x,y), ...]
#     obstacle_positions: 장애물 리스트 [(x,y), (x,y), ...]
#     lookahead: 몇 단계까지 미리 볼지 (default 2단계)
#     """
#     from heapq import heappush, heappop
#     import numpy as np

#     grid_size = 50
#     directions = [(-1, 0), (1, 0), (0, -1), (0, 1)]
#     trash_positions = trash_positions.copy()
#     obstacle_positions_set = set(obstacle_positions)

#     path = [start]
#     current_position = start

#     def heuristic(a, b):
#         # 단순 유클리디언 거리
#         return np.linalg.norm(np.array(a) - np.array(b))

#     def a_star(start, goal):
#         open_set = []
#         heappush(open_set, (0 + heuristic(start, goal), 0, start, [start]))
#         closed_set = set()

#         while open_set:
#             est_total_cost, cost_so_far, current, current_path = heappop(open_set)

#             if current == goal:
#                 return current_path

#             if current in closed_set:
#                 continue

#             closed_set.add(current)

#             for d in directions:
#                 neighbor = (current[0] + d[0], current[1] + d[1])
#                 if (0 <= neighbor[0] < grid_size and 0 <= neighbor[1] < grid_size and neighbor not in obstacle_positions_set):
#                     if neighbor not in closed_set:
#                         new_cost = cost_so_far + 1
#                         est_total = new_cost + heuristic(neighbor, goal)
#                         heappush(open_set, (est_total, new_cost, neighbor, current_path + [neighbor]))

#         return None

#     while trash_positions:
#         candidates = []

#         for target in trash_positions:
#             total_estimated_cost = 0
#             simulated_position = target
#             visited = [target]

#             # 첫 번째 경로는 실제 A*로 계산
#             route = a_star(current_position, target)
#             if route is None:
#                 continue
#             total_estimated_cost += len(route)

#             # lookahead (앞으로 몇 번 더)
#             for _ in range(lookahead - 1):
#                 # 다음 후보 중 가장 가까운거 탐색
#                 remaining_trash = [t for t in trash_positions if t not in visited]
#                 if not remaining_trash:
#                     break

#                 next_target = min(remaining_trash, key=lambda p: heuristic(simulated_position, p))
#                 total_estimated_cost += heuristic(simulated_position, next_target)
#                 simulated_position = next_target
#                 visited.append(next_target)

#             candidates.append((total_estimated_cost, target))

#         if not candidates:
#             break

#         # 예상 총 거리가 가장 작은 후보 선택
#         _, best_target = min(candidates)

#         # 실제 이동
#         best_route = a_star(current_position, best_target)
#         if best_route:
#             path.extend(best_route[1:])  # 첫 점(current_position)은 이미 있으니까 제외
#             current_position = best_target
#             trash_positions.remove(best_target)

#     return path

# # ================================================
# # 점수 알고리즘
# # ================================================
# import heapq
# import numpy as np

# def score_algo(start, trash_positions, obstacle_positions):
#     grid_size = 50
#     directions = [(-1,0), (1,0), (0,-1), (0,1), (-1,-1), (-1,1), (1,-1), (1,1)]
#     path = []
#     current_pos = start

#     def heuristic(a, b):
#         return np.linalg.norm(np.array(a) - np.array(b))

#     def a_star(start, goal, obstacles):
#         open_set = []
#         heapq.heappush(open_set, (0 + heuristic(start, goal), 0, start, [start]))
#         visited = set()

#         while open_set:
#             est_total_cost, cost_so_far, current, current_path = heapq.heappop(open_set)

#             if current == goal:
#                 return current_path

#             if current in visited:
#                 continue

#             visited.add(current)

#             for d in directions:
#                 neighbor = (current[0] + d[0], current[1] + d[1])
#                 if (0 <= neighbor[0] < grid_size and 0 <= neighbor[1] < grid_size and neighbor not in obstacles):
#                     if neighbor not in visited:
#                         new_cost = cost_so_far + 1
#                         est_total = new_cost + heuristic(neighbor, goal)
#                         heapq.heappush(open_set, (est_total, new_cost, neighbor, current_path + [neighbor]))
#         return None

#     while trash_positions:
#         # 방향별 점수 계산
#         scores = []
#         for d in directions:
#             score = 0
#             for trash in trash_positions:
#                 vector = (trash[0] - current_pos[0], trash[1] - current_pos[1])
#                 dot = d[0]*vector[0] + d[1]*vector[1]
#                 if dot > 0:  # 해당 방향
#                     dist = heuristic(current_pos, trash)
#                     score += 1 / (dist + 1e-5)
#             scores.append(score)

#         # 최고 점수 방향 선택
#         best_dir_idx = np.argmax(scores)
#         best_dir = directions[best_dir_idx]

#         # 해당 방향으로 가장 가까운 쓰레기 찾기
#         candidates = [p for p in trash_positions if (p[0] - current_pos[0])*best_dir[0] >=0 and (p[1] - current_pos[1])*best_dir[1] >=0]
#         if not candidates:
#             candidates = trash_positions
        
#         next_trash = min(candidates, key=lambda p: heuristic(current_pos, p))

#         # 경로 탐색
#         route = a_star(current_pos, next_trash, set(obstacle_positions))

#         if route:
#             path.extend(route[1:])
#             current_pos = next_trash
#             trash_positions.remove(next_trash)
#         else:
#             trash_positions.remove(next_trash)

#     return path

# # ================================================
# # 알고리즘 성능 평가 함수
# # ================================================
# def evaluate_algorithm(algorithm, start, trash_positions, obstacle_positions):
#     start_time = time.time()
#     results = algorithm(start, trash_positions.copy(), obstacle_positions)
#     end_time = time.time()
    
#     collection_rate = len(set(results) & set(trash_positions)) / len(trash_positions) if trash_positions else 0
#     collision_count = len([pos for pos in results if pos in obstacle_positions])
#     search_distance = len(results)
#     elapsed_time = end_time - start_time
    
#     return {
#         "collection_rate": collection_rate,
#         "collision_count": collision_count,
#         "search_distance": search_distance,
#         "elapsed_time": elapsed_time
#     }

# # ================================================
# # 결과 출력 함수
# # ================================================
# def print_results(results):
#     for name, metrics in results.items():
#         print(f"{name} Algorithm:")
#         print(f"  수집률: {metrics['collection_rate']:.4f}")
#         print(f"  충돌 횟수: {metrics['collision_count']}")
#         print(f"  탐색 거리: {metrics['search_distance']}")
#         print(f"  실행 시간: {metrics['elapsed_time']:.4f} seconds")
#         print()

# # ================================================
# # 결과 저장 함수
# # ================================================
# save_folder = "algo_new_test_results"
# os.makedirs(save_folder, exist_ok=True)

# algorithm_names = ["❤️Fusion", "💚Predictive", "💙Score"]

# def save_partial_results(all_results, save_count):
#     rows = []
#     for i, result in enumerate(all_results):
#         for algo in algorithm_names:
#             row = {
#                 "Index": i,
#                 "Algorithm": algo,
#                 "CollectionRate": result[algo]["collection_rate"],
#                 "CollisionCount": result[algo]["collision_count"],
#                 "SearchDistance": result[algo]["search_distance"],
#                 "ElapsedTime": result[algo]["elapsed_time"]
#             }
#             rows.append(row)
#     df = pd.DataFrame(rows)
#     df.to_csv(f"{save_folder}/algorithm_test_results_{save_count}.csv", index=False, encoding='utf-8-sig')

# # ================================================
# # 메인 실행
# # ================================================
# all_results = []
# batch_size = 500
# save_count = 1
# total = len(data)

# for index, row in data.iterrows():
#     trash_positions, obstacle_positions = generate_trash_obstacles_with_environment(50, row)
#     start = (0, 0)

#     fusion_results = evaluate_algorithm(fusion_algo, start, trash_positions, obstacle_positions)
#     predictive_results = evaluate_algorithm(predictive_algo, start, trash_positions, obstacle_positions)
#     score_results = evaluate_algorithm(score_algo, start, trash_positions, obstacle_positions)

#     results = {
#         "❤️Fusion": fusion_results,
#         "💚Predictive": predictive_results,
#         "💙Score": score_results
#     }
    
#     all_results.append(results)
    
#     print()
#     print("================================================")
#     print(f"Data Index {index+1}/{total}: ({(index+1)/total*100:.2f}%)")
#     print()
#     print_results(results)

#     if (index + 1) % batch_size == 0:
#         save_partial_results(all_results, save_count)
#         print(f"\n✅ {batch_size}개 저장 완료: algorithm_test_results_{save_count}.csv\n")
#         all_results.clear()
#         save_count += 1

# if all_results:
#     save_partial_results(all_results, save_count)
#     print("\n================================================")
#     print(f"✅ TEST RESULT FILE SAVED: algorithm_test_results_{save_count}.csv")
#     print("================================================")
#     print()

# # ================================================
# # 평균 결과 계산
# # ================================================
# print("\n================================================")
# print("✅ TEST RESULT AVERAGE")
# file_list = glob.glob(f"{save_folder}/algorithm_test_results_*.csv")

# dfs = []
# for file in file_list:
#     dfs.append(pd.read_csv(file))

# full_df = pd.concat(dfs, ignore_index=True)

# for algo in algorithm_names:
#     algo_df = full_df[full_df["Algorithm"] == algo]
#     print(f"\n=== {algo} Algorithm ===")
#     print(f"  수집률 평균: {algo_df['CollectionRate'].mean():.4f}")
#     print(f"  충돌 횟수 평균: {algo_df['CollisionCount'].mean():.4f}")
#     print(f"  탐색 거리 평균: {algo_df['SearchDistance'].mean():.4f}")
#     print(f"  실행 시간 평균: {algo_df['ElapsedTime'].mean():.4f} seconds")

# print()
















import pandas as pd
import random
import heapq
import numpy as np
import time
import os
import glob

# ================================================
# 작업 디렉토리 설정
# ================================================
os.chdir("C:/")
os.makedirs("algo_new_test_results", exist_ok=True)

# ================================================
# CSV 데이터 로드
# ================================================
def load_data(file_path):
    return pd.read_csv(file_path)

data = load_data('2023.csv')

# ================================================
# 환경 기반 쓰레기 및 장애물 생성 함수
# ================================================
def generate_trash_obstacles_with_environment(grid_size, env_row):
    # === 환경 데이터 읽기 ===
    wind_speed = env_row['풍속(m/s)']
    wind_direction = env_row['풍향(deg)']
    gust_speed = env_row['GUST풍속(m/s)']
    max_wave_height = env_row['최대파고(m)']
    significant_wave_height = env_row['유의파고(m)']
    mean_wave_height = env_row['평균파고(m)']
    wave_period = env_row['파주기(sec)']
    wave_direction = env_row['파향(deg)']

    # === NaN 처리 (기본값 설정) ===
    if pd.isna(wind_speed):
        wind_speed = 0
    if pd.isna(wind_direction):
        wind_direction = 0
    if pd.isna(gust_speed):
        gust_speed = 0
    if pd.isna(max_wave_height):
        max_wave_height = 0
    if pd.isna(significant_wave_height):
        significant_wave_height = 0
    if pd.isna(mean_wave_height):
        mean_wave_height = 0
    if pd.isna(wave_period):
        wave_period = 0
    if pd.isna(wave_direction):
        wave_direction = 0

    # === 쓰레기와 장애물 개수 결정 ===
    base_trash = 50
    base_obstacles = 10

    trash_count = base_trash + int(wind_speed * 5) + int(significant_wave_height * 10)
    obstacle_count = base_obstacles + int(gust_speed * 2) + int(max_wave_height * 5)

    trash_count = min(trash_count, 300)
    obstacle_count = min(obstacle_count, 50)

    # === 쓰레기 배치 (바람 방향 고려) ===
    trash_positions = []
    for _ in range(trash_count):
        base_x = random.randint(0, grid_size-1)
        base_y = random.randint(0, grid_size-1)
        
        offset_distance = random.randint(0, 5)
        offset_x = int(np.cos(np.radians(wind_direction)) * offset_distance)
        offset_y = int(np.sin(np.radians(wind_direction)) * offset_distance)

        new_x = max(0, min(grid_size-1, base_x + offset_x))
        new_y = max(0, min(grid_size-1, base_y + offset_y))
        
        trash_positions.append((new_x, new_y))

    # === 장애물 배치 (파향 고려) ===
    obstacle_positions = []
    for _ in range(obstacle_count):
        base_x = random.randint(0, grid_size-1)
        base_y = random.randint(0, grid_size-1)
        
        offset_distance = random.randint(0, 3)
        offset_x = int(np.cos(np.radians(wave_direction)) * offset_distance)
        offset_y = int(np.sin(np.radians(wave_direction)) * offset_distance)

        new_x = max(0, min(grid_size-1, base_x + offset_x))
        new_y = max(0, min(grid_size-1, base_y + offset_y))

        obstacle_positions.append((new_x, new_y))

    return trash_positions, obstacle_positions

# ================================================
# 퓨전 알고리즘
# ================================================
import heapq
import numpy as np

def fusion_algo(start, trash_positions, obstacle_positions):
    grid_size = 50
    directions = [(-1,0), (1,0), (0,-1), (0,1)]
    path = []
    current_pos = start

    def heuristic(a, b):
        return np.linalg.norm(np.array(a) - np.array(b))

    def a_star(start, goal, obstacles):
        open_set = []
        heapq.heappush(open_set, (0 + heuristic(start, goal), 0, start, [start]))
        visited = set()

        while open_set:
            est_total_cost, cost_so_far, current, current_path = heapq.heappop(open_set)

            if current == goal:
                return current_path

            if current in visited:
                continue

            visited.add(current)

            for d in directions:
                neighbor = (current[0] + d[0], current[1] + d[1])
                if (0 <= neighbor[0] < grid_size and 0 <= neighbor[1] < grid_size and neighbor not in obstacles):
                    if neighbor not in visited:
                        new_cost = cost_so_far + 1
                        est_total = new_cost + heuristic(neighbor, goal)
                        heapq.heappush(open_set, (est_total, new_cost, neighbor, current_path + [neighbor]))
        return None

    while trash_positions:
        # 가장 가까운 쓰레기 선택 (Greedy)
        next_trash = min(trash_positions, key=lambda p: heuristic(current_pos, p))

        # 경로 탐색 (A*)
        route = a_star(current_pos, next_trash, set(obstacle_positions))

        # 만약 경로를 못 찾으면 Dijkstra 스타일로 brute-force
        if route is None:
            candidates = []
            for trash in trash_positions:
                route_try = a_star(current_pos, trash, set(obstacle_positions))
                if route_try:
                    candidates.append((len(route_try), trash, route_try))
            if candidates:
                candidates.sort()
                _, next_trash, route = candidates[0]
            else:
                trash_positions.remove(next_trash)
                continue

        # 경로 추가
        path.extend(route[1:])
        current_pos = next_trash
        trash_positions.remove(next_trash)

        # 주변 센싱 (D*처럼 재탐색)
        nearby_trashes = [p for p in trash_positions if heuristic(current_pos, p) <= 5]
        if nearby_trashes:
            for p in nearby_trashes:
                route_to_nearby = a_star(current_pos, p, set(obstacle_positions))
                if route_to_nearby:
                    path.extend(route_to_nearby[1:])
                    current_pos = p
                    trash_positions.remove(p)

    return path

# # ================================================
# # 예측 알고리즘
# # ================================================
# def predictive_algo(start, trash_positions, obstacle_positions, lookahead=2):
#     """
#     Predictive Greedy A* Algorithm
#     start: 시작 위치 (x, y)
#     trash_positions: 수거해야할 쓰레기 리스트 [(x,y), (x,y), ...]
#     obstacle_positions: 장애물 리스트 [(x,y), (x,y), ...]
#     lookahead: 몇 단계까지 미리 볼지 (default 2단계)
#     """
#     from heapq import heappush, heappop
#     import numpy as np

#     grid_size = 50
#     directions = [(-1, 0), (1, 0), (0, -1), (0, 1)]
#     trash_positions = trash_positions.copy()
#     obstacle_positions_set = set(obstacle_positions)

#     path = [start]
#     current_position = start

#     def heuristic(a, b):
#         # 단순 유클리디언 거리
#         return np.linalg.norm(np.array(a) - np.array(b))

#     def a_star(start, goal):
#         open_set = []
#         heappush(open_set, (0 + heuristic(start, goal), 0, start, [start]))
#         closed_set = set()

#         while open_set:
#             est_total_cost, cost_so_far, current, current_path = heappop(open_set)

#             if current == goal:
#                 return current_path

#             if current in closed_set:
#                 continue

#             closed_set.add(current)

#             for d in directions:
#                 neighbor = (current[0] + d[0], current[1] + d[1])
#                 if (0 <= neighbor[0] < grid_size and 0 <= neighbor[1] < grid_size and neighbor not in obstacle_positions_set):
#                     if neighbor not in closed_set:
#                         new_cost = cost_so_far + 1
#                         est_total = new_cost + heuristic(neighbor, goal)
#                         heappush(open_set, (est_total, new_cost, neighbor, current_path + [neighbor]))

#         return None

#     while trash_positions:
#         candidates = []

#         for target in trash_positions:
#             total_estimated_cost = 0
#             simulated_position = target
#             visited = [target]

#             # 첫 번째 경로는 실제 A*로 계산
#             route = a_star(current_position, target)
#             if route is None:
#                 continue
#             total_estimated_cost += len(route)

#             # lookahead (앞으로 몇 번 더)
#             for _ in range(lookahead - 1):
#                 # 다음 후보 중 가장 가까운거 탐색
#                 remaining_trash = [t for t in trash_positions if t not in visited]
#                 if not remaining_trash:
#                     break

#                 next_target = min(remaining_trash, key=lambda p: heuristic(simulated_position, p))
#                 total_estimated_cost += heuristic(simulated_position, next_target)
#                 simulated_position = next_target
#                 visited.append(next_target)

#             candidates.append((total_estimated_cost, target))

#         if not candidates:
#             break

#         # 예상 총 거리가 가장 작은 후보 선택
#         _, best_target = min(candidates)

#         # 실제 이동
#         best_route = a_star(current_position, best_target)
#         if best_route:
#             path.extend(best_route[1:])  # 첫 점(current_position)은 이미 있으니까 제외
#             current_position = best_target
#             trash_positions.remove(best_target)

#     return path

# ================================================
# 점수 알고리즘
# ================================================
import heapq
import numpy as np

def score_algo(start, trash_positions, obstacle_positions):
    grid_size = 50
    directions = [(-1,0), (1,0), (0,-1), (0,1), (-1,-1), (-1,1), (1,-1), (1,1)]
    path = []
    current_pos = start

    def heuristic(a, b):
        return np.linalg.norm(np.array(a) - np.array(b))

    def a_star(start, goal, obstacles):
        open_set = []
        heapq.heappush(open_set, (0 + heuristic(start, goal), 0, start, [start]))
        visited = set()

        while open_set:
            est_total_cost, cost_so_far, current, current_path = heapq.heappop(open_set)

            if current == goal:
                return current_path

            if current in visited:
                continue

            visited.add(current)

            for d in directions:
                neighbor = (current[0] + d[0], current[1] + d[1])
                if (0 <= neighbor[0] < grid_size and 0 <= neighbor[1] < grid_size and neighbor not in obstacles):
                    if neighbor not in visited:
                        new_cost = cost_so_far + 1
                        est_total = new_cost + heuristic(neighbor, goal)
                        heapq.heappush(open_set, (est_total, new_cost, neighbor, current_path + [neighbor]))
        return None

    while trash_positions:
        # 방향별 점수 계산
        scores = []
        for d in directions:
            score = 0
            for trash in trash_positions:
                vector = (trash[0] - current_pos[0], trash[1] - current_pos[1])
                dot = d[0]*vector[0] + d[1]*vector[1]
                if dot > 0:  # 해당 방향
                    dist = heuristic(current_pos, trash)
                    score += 1 / (dist + 1e-5)
            scores.append(score)

        # 최고 점수 방향 선택
        best_dir_idx = np.argmax(scores)
        best_dir = directions[best_dir_idx]

        # 해당 방향으로 가장 가까운 쓰레기 찾기
        candidates = [p for p in trash_positions if (p[0] - current_pos[0])*best_dir[0] >=0 and (p[1] - current_pos[1])*best_dir[1] >=0]
        if not candidates:
            candidates = trash_positions
        
        next_trash = min(candidates, key=lambda p: heuristic(current_pos, p))

        # 경로 탐색
        route = a_star(current_pos, next_trash, set(obstacle_positions))

        if route:
            path.extend(route[1:])
            current_pos = next_trash
            trash_positions.remove(next_trash)
        else:
            trash_positions.remove(next_trash)

    return path

# ================================================
# 알고리즘 성능 평가 함수
# ================================================
def evaluate_algorithm(algorithm, start, trash_positions, obstacle_positions):
    start_time = time.time()
    results = algorithm(start, trash_positions.copy(), obstacle_positions)
    end_time = time.time()
    
    collection_rate = len(set(results) & set(trash_positions)) / len(trash_positions) if trash_positions else 0
    collision_count = len([pos for pos in results if pos in obstacle_positions])
    search_distance = len(results)
    elapsed_time = end_time - start_time
    
    return {
        "collection_rate": collection_rate,
        "collision_count": collision_count,
        "search_distance": search_distance,
        "elapsed_time": elapsed_time
    }

# ================================================
# 결과 출력 함수
# ================================================
def print_results(results):
    for name, metrics in results.items():
        print(f"{name} Algorithm:")
        print(f"  수집률: {metrics['collection_rate']:.4f}")
        print(f"  충돌 횟수: {metrics['collision_count']}")
        print(f"  탐색 거리: {metrics['search_distance']}")
        print(f"  실행 시간: {metrics['elapsed_time']:.4f} seconds")
        print()

# ================================================
# 결과 저장 함수
# ================================================
save_folder = "algo_new_test_results"
os.makedirs(save_folder, exist_ok=True)

algorithm_names = ["❤️Fusion", "💙Score"]

def save_partial_results(all_results, save_count):
    rows = []
    for i, result in enumerate(all_results):
        for algo in algorithm_names:
            row = {
                "Index": i,
                "Algorithm": algo,
                "CollectionRate": result[algo]["collection_rate"],
                "CollisionCount": result[algo]["collision_count"],
                "SearchDistance": result[algo]["search_distance"],
                "ElapsedTime": result[algo]["elapsed_time"]
            }
            rows.append(row)
    df = pd.DataFrame(rows)
    df.to_csv(f"{save_folder}/algorithm_test_results_{save_count}.csv", index=False, encoding='utf-8-sig')

# ================================================
# 메인 실행
# ================================================
all_results = []
batch_size = 500
save_count = 1
total = len(data)

for index, row in data.iterrows():
    trash_positions, obstacle_positions = generate_trash_obstacles_with_environment(50, row)
    start = (0, 0)

    fusion_results = evaluate_algorithm(fusion_algo, start, trash_positions, obstacle_positions)
    # predictive_results = evaluate_algorithm(predictive_algo, start, trash_positions, obstacle_positions)
    score_results = evaluate_algorithm(score_algo, start, trash_positions, obstacle_positions)

    results = {
        "❤️Fusion": fusion_results,
        # "💚Predictive": predictive_results,
        "💙Score": score_results
    }
    
    all_results.append(results)
    
    print()
    print("================================================")
    print(f"Data Index {index+1}/{total}: ({(index+1)/total*100:.2f}%)")
    print()
    print_results(results)

    if (index + 1) % batch_size == 0:
        save_partial_results(all_results, save_count)
        print(f"\n✅ {batch_size}개 저장 완료: algorithm_test_results_{save_count}.csv\n")
        all_results.clear()
        save_count += 1

if all_results:
    save_partial_results(all_results, save_count)
    print("\n================================================")
    print(f"✅ TEST RESULT FILE SAVED: algorithm_test_results_{save_count}.csv")
    print("================================================")
    print()

# ================================================
# 평균 결과 계산
# ================================================
print("\n================================================")
print("✅ TEST RESULT AVERAGE")
file_list = glob.glob(f"{save_folder}/algorithm_test_results_*.csv")

dfs = []
for file in file_list:
    dfs.append(pd.read_csv(file))

full_df = pd.concat(dfs, ignore_index=True)

for algo in algorithm_names:
    algo_df = full_df[full_df["Algorithm"] == algo]
    print(f"\n=== {algo} Algorithm ===")
    print(f"  수집률 평균: {algo_df['CollectionRate'].mean():.4f}")
    print(f"  충돌 횟수 평균: {algo_df['CollisionCount'].mean():.4f}")
    print(f"  탐색 거리 평균: {algo_df['SearchDistance'].mean():.4f}")
    print(f"  실행 시간 평균: {algo_df['ElapsedTime'].mean():.4f} seconds")

print()